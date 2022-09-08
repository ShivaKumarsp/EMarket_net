﻿using EMarket.BLL.Comman_Class;
using EMarket.BLL.Comman_Class.Interface;
using EMarket.BLL.Interfaces.Customer;
using EMarket.DLL.Interfaces.Customer;
using EMarket.Entities;
using EMarket.Entities.Customer;
using EMarketDTO.Customer;
using LiteX.DbHelper.Core;
using LiteX.DbHelper.Npgsql;
using Razorpay.Api;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace EMarket.BLL.EMarket_Service.Customer
{
    public class CartCheckPlaceOrder_Service : ICartCheckPlaceOrder_Service
    {
        string key = "rzp_test_B9w0QIw6AHPt3x";
        string secret = "YUMCvoUCim2qkrR6gSPIOrY5";
        Db_Connection conn = new Db_Connection();
        PostgreSqlContext _context;
        ISqlClass _sql;
        IErrorClass _error;
        int status = 0;

        public CartCheckPlaceOrder_Service(PostgreSqlContext context, ISqlClass sql, IErrorClass error)
        {
            _context = context;
            _sql = sql;
            _error = error;
        }

        public CartCheckPlaceOrderDTO get_data(CartCheckPlaceOrderDTO dto)
        {
            var page_form = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
            string methodname = "CartCheckOrder_Service/get_data";
            var Params = new DbParameter[] { };
            IDbHelper _dbHelper = new NpgsqlHelper(conn.ConnectionString);
            try
            {
                // Check And Update Quantity
                var dddd = DateTime.Now.AddMinutes(-2);
                var resultqty = _context.Payment_Order_QtyDMO_con.Where(a => a.order_on <= dddd).ToList();
                if (resultqty.Count > 0)
                {
                    foreach (var item in resultqty)
                    {
                        var dbParams2 = new DbParameter[]
                    {
                    DbHelper.CreateParameter("in_payment_order_id", item.payment_order_id),
                    DbHelper.CreateParameter("in_item_id", item.item_id),
                    DbHelper.CreateParameter("in_quantity", item.quantity),
                    DbHelper.CreateParameter("in_order_on", item.order_on)
                    };
                        Params = dbParams2;
                        var spName = "call buy_quantity_update(:in_payment_order_id, :in_item_id,:in_quantity,:in_order_on)";
                        status = _dbHelper.ExecuteNonQuery(spName, CommandType.Text, dbParams2);
                    }
                }

                var customer = _context.Customer_Profile_con.Where(a => a.user_id == dto.user_id).SingleOrDefault();
                dto.customer_id = customer.customer_id;

                List<long> itemid = new List<long>();
                var cartlist = _context.Cart_List_con.Where(a => a.userid == dto.user_id && a.customer_id == dto.customer_id).ToList();

                foreach (var item in cartlist)
                {
                    itemid.Add(item.item_id);
                }

                var allcartlist = (from a in _context.Master_Product_con
                                   from b in _context.Product_ItemDMO_con
                                   where itemid.Contains(b.item_id) && a.product_id == b.product_id
                                   select new Shopping_CartDTO
                                   {
                                       product_name = a.product_name,
                                       product_id = a.product_id,
                                       item_id = b.item_id,
                                       selling_price = b.listing_price,
                                       totquantity = b.min_quantity,
                                       imageurl = a.image_path,
                                   }).Distinct().ToList();

                foreach (var item in allcartlist)
                {
                    if (item.totquantity == 0)
                    {
                        var removecart = _context.Cart_List_con.Where(a => a.userid == dto.user_id && a.customer_id == dto.customer_id && a.product_id == item.product_id && a.item_id == item.item_id).SingleOrDefault();
                        _context.Remove(removecart);
                    }

                }
                _context.SaveChanges();

                // get cart_item
                var dbParams = new DbParameter[]
                    {
                      DbHelper.CreateParameter("in_languageid", dto.language_id),
                      DbHelper.CreateParameter("in_user_id", dto.user_id),
                      DbHelper.CreateParameter("in_customer_id", dto.customer_id),
                    };
                Params = dbParams;
                dto.procedure_name = "fn_get_cart_item";
                dto.mycartlist = _sql.fn_get_list(dto.procedure_name, dbParams);

                dto.shipping_address_list = _context.Customer_Shipping_Address_DMO_con.Where(a => a.user_id == dto.user_id).OrderByDescending(a => a.shippingaddress_id).ToArray();


            }
            catch (Exception ex)
            {
                _error.errorlog(ex, dto.user_id, methodname, dto.ipAddress, dto.apitype, page_form, dto.procedure_name, Params);
            }
            finally
            {
                _error.audit_log_txr(dto.user_id, methodname, page_form);
            }
            return dto;
        }


        public CartCheckPlaceOrderDTO CheckOut_online(CartCheckPlaceOrderDTO dto)
        {
            var page_form = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
            string methodname = "CartCheckPlaceOrder_Service/CheckOut_online";
            TimeZoneInfo INDIAN_ZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            IDbHelper _dbHelper = new NpgsqlHelper(conn.ConnectionString);
            var Params = new DbParameter[] { };
            DateTime indianTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, INDIAN_ZONE);
            long orderid = 0;
            try
            {
                // Check And Update Quantity
                var dddd = DateTime.Now.AddMinutes(-2);
                var resultqty = _context.Payment_Order_QtyDMO_con.Where(a => a.order_on <= dddd).ToList();
                if (resultqty.Count > 0)
                {
                    foreach (var item in resultqty)
                    {
                        var dbParams2 = new DbParameter[]
                  {
                    DbHelper.CreateParameter("in_payment_order_id", item.payment_order_id),
                    DbHelper.CreateParameter("in_item_id", item.item_id),
                    DbHelper.CreateParameter("in_quantity", item.quantity),
                    DbHelper.CreateParameter("in_order_on", item.order_on)
                  };
                        Params = dbParams2;
                        var spName = "call buy_quantity_update(:in_payment_order_id, :in_item_id,:in_quantity,:in_order_on)";
                        status = _dbHelper.ExecuteNonQuery(spName, CommandType.Text, dbParams2);
                    }
                }


                foreach (var item in dto.ordercartlist)
                {
                    var itmqty = _context.Product_ItemDMO_con.Where(a => a.item_id == item.itemid).ToList();
                    if (item.quantity > itmqty[0].quantity)
                    {
                        dto.ret_itemname = itmqty[0].item_name;
                        dto.ret_itemqty = itmqty[0].quantity;
                        dto.status = "Failed";
                        return dto;
                    }
                }
                var customer = _context.Customer_Profile_con.Where(a => a.user_id == dto.user_id).SingleOrDefault();
                dto.customer_id = customer.customer_id;

                //var checkdata = _context.Customer_OrderDMO_con.ToList();
                //if (checkdata.Count != 0)
                //{
                //    orderid = _context.Customer_OrderDMO_con.Max(x => x.order_id);
                //    orderid++;
                //}
                //else
                //{
                //    orderid = 100;
                //}

                //var billinadd = _context.Customer_Shipping_Address_DMO_con.Where(a => a.user_id == dto.user_id && a.default_address == true).FirstOrDefault();

                //Customer_OrderDMO co = new Customer_OrderDMO();
                //co.payment_method_id = 2;
                //co.set_paid = true;
                //co.status_id = 1;
                //co.user_id = dto.user_id;
                //co.customer_id = dto.customer_id;
                //co.email = billinadd.email;
                //co.phone_number = billinadd.mobile;
                //co.billing_address_id = billinadd.shippingaddress_id;
                ////co.shipping_address_id = billinadd.shippingaddress_id;
                //co.is_gst_available = true;
                //co.delivery_charge = dto.delivery_charge;
                //co.gross_amount = dto.gross_amount;
                //co.discount_id = 1;
                //co.tax_calculate_id = 1;
                //co.gst_number = "1234";
                //co.payable_amount = dto.payable_amount;
                //co.total_order_amount = dto.total_order_amount;
                //co.discount_amount = dto.discount_amount;
                //co.tax_amount = dto.tax_amount;
                //co.payment_status = "Pending";
                //co.order_date = indianTime;
                //_context.Add(co);
                //_context.SaveChanges();

                //Order_Shipping_AddressDMO ord = new Order_Shipping_AddressDMO();
                //ord.order_id = orderid;
                //ord.user_id = dto.user_id;
                //ord.customer_id = dto.customer_id;
                //ord.customer_name = billinadd.name;
                //ord.address_line_1 = billinadd.address_line_1;
                //ord.address_line_2 = billinadd.address_line_2;
                //ord.city = billinadd.city;
                //ord.country_id = 1;
                //ord.state_id = 4;
                //ord.pincode = billinadd.pincode;
                //ord.land_mark = billinadd.land_mark;
                //ord.mobile = billinadd.mobile;
                //ord.email_id = billinadd.email;
                //_context.Add(ord);
                //var ss = _context.SaveChanges();

                //var ordid = co.order_id;
                //foreach (var item in dto.ordercartlist)
                //{
                //    Customer_Order_ItemDMO dmo = new Customer_Order_ItemDMO();
                //    dmo.order_id = ordid;
                //    dmo.product_id = item.productid;
                //    dmo.item_id = item.itemid;
                //    dmo.quantity = item.quantity;
                //    dmo.selling_price = item.selling_price;
                //    dmo.mrp = item.mrp;
                //    dmo.status_id = 1;
                //    dmo.order_accept_status = "Pending";
                //    _context.Add(dmo);
                //    _context.SaveChanges();

                //    Order_Item_txnDMO ddm = new Order_Item_txnDMO();
                //    ddm.order_id = ordid;
                //    ddm.order_item_id = dmo.order_item_id;
                //    ddm.order_txn_status_id = 1;
                //    ddm.status_details = "";
                //    ddm.is_activity_success = false;
                //    ddm.created_by = dto.user_id;
                //    ddm.created_on = indianTime;
                //    _context.Add(ddm);
                //    _context.SaveChanges();
                //}

                var billinadd = _context.Customer_Shipping_Address_DMO_con.Where(a => a.user_id == dto.user_id && a.default_address == true).FirstOrDefault();




                //dto.order_id = ordid;
                dto.customer_name = billinadd.name;
                dto.email = billinadd.email;
                dto.mobile = billinadd.mobile;
                Dictionary<String, object> transfersnotes = new Dictionary<String, object>();

                transfersnotes.Add("notes_1", billinadd.name);
                transfersnotes.Add("notes_2", billinadd.address_line_1);
                transfersnotes.Add("notes_3", billinadd.address_line_1);
                transfersnotes.Add("notes_4", billinadd.address_line_2);
                transfersnotes.Add("notes_5", billinadd.city);
                transfersnotes.Add("notes_6", billinadd.pincode.ToString());
                dto.inputvalue = Newtonsoft.Json.JsonConvert.SerializeObject(transfersnotes);
                var amt = dto.payable_amount;
                Dictionary<string, object> input = new Dictionary<string, object>();

                input.Add("amount", amt * 100);
                input.Add("currency", "INR");
                input.Add("payment_capture", 1);
                input.Add("notes", transfersnotes);

                RazorpayClient client = new RazorpayClient(key, secret);
                Razorpay.Api.Order order = client.Order.Create(input);
                dto.payment_orderid = order["id"].ToString();

                foreach (var item in dto.ordercartlist)
                {
                    Payment_Order_QtyDMO dmo1 = new Payment_Order_QtyDMO();
                    dmo1.item_id = item.itemid;
                    dmo1.payment_order_id = dto.payment_orderid;
                    dmo1.quantity = item.quantity;
                    dmo1.order_on = DateTime.Now;
                    _context.Add(dmo1);

                    var prdvariuant = _context.Product_ItemDMO_con.Where(a => a.product_id == item.productid && a.item_id == item.itemid).SingleOrDefault();
                    prdvariuant.quantity = prdvariuant.quantity - item.quantity;
                    _context.Update(prdvariuant);

                }
                _context.SaveChanges();

                Online_Payment_TransactionDMO pt = new Online_Payment_TransactionDMO();
                pt.customer_id = dto.customer_id;
                pt.user_id = dto.user_id;
                pt.payment_order_id = dto.payment_orderid;
                pt.amount = dto.payable_amount;
                pt.mode_of_payment = "Online";
                pt.gateway_name = "razorpay";
                pt.payment_status = "Pending";
                pt.payment_transaction_id = "";
                pt.payment_date_time = indianTime;
                _context.Add(pt);

                var ss1 = _context.SaveChanges();
                if (ss1 > 0)
                {
                    dto.status_flg = true;
                }

                else
                {
                    dto.status_flg = false;
                }


            }
            catch (Exception ex)
            {

                _error.errorlog_add(ex, dto.user_id, methodname, dto.ipAddress, dto.apitype, page_form, "Linqe", dto.inputvalue);
            }
            finally
            {
                _error.audit_log_txr(dto.user_id, methodname, page_form);
            }
            return dto;

        }
        public CartCheckPlaceOrderDTO CheckOut_POD(CartCheckPlaceOrderDTO dto)
        {
            var page_form = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
            string methodname = "CartCheckPlaceOrder_Repository/CheckOut_POD";
            IDbHelper _dbHelper = new NpgsqlHelper(conn.ConnectionString);
            var Params = new DbParameter[] { };
            bool ret = false;
            try
            {
                _error.audit_log_txr(dto.user_id, methodname, page_form);
                var customer = _context.Customer_Profile_con.Where(a => a.user_id == dto.user_id).SingleOrDefault();
                dto.customer_id = customer.customer_id;
                TimeZoneInfo INDIAN_ZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                DateTime indianTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, INDIAN_ZONE);

                // Check And Update Quantity
                var dddd = DateTime.Now.AddMinutes(-2);
                var resultqty = _context.Payment_Order_QtyDMO_con.Where(a => a.order_on <= dddd).ToList();
                if (resultqty.Count > 0)
                {
                    foreach (var item in resultqty)
                    {
                        var dbParams2 = new DbParameter[]
                    {
                    DbHelper.CreateParameter("in_payment_order_id", item.payment_order_id),
                    DbHelper.CreateParameter("in_item_id", item.item_id),
                    DbHelper.CreateParameter("in_quantity", item.quantity),
                    DbHelper.CreateParameter("in_order_on", item.order_on)
                    };
                    Params = dbParams2;
                    var spName = "call buy_quantity_update(:in_payment_order_id, :in_item_id,:in_quantity,:in_order_on)";
                    status = _dbHelper.ExecuteNonQuery(spName, CommandType.Text, dbParams2);
                    }
                }


                long orderid = 0;
                var crtqq = _context.Cart_List_con.Where(a => a.customer_id == dto.customer_id).ToList();
                if (crtqq.Count == 0)
                {
                    return dto;
                }
                var pod = _context.POD_CheckDMO_con.Where(a => a.customer_id == dto.customer_id).ToList();
                if (pod.Count == 0)
                {
                    POD_CheckDMO dm = new POD_CheckDMO();
                    dm.customer_id = dto.customer_id;
                    dm.pod_time = DateTime.Now;
                    _context.Add(dm);
                    _context.SaveChanges();
                }
                else if (pod.Count > 0)
                {
      
                    var dbParams = new DbParameter[]
                        {
                         DbHelper.CreateParameter("in_customer_id", dto.customer_id)
                        };
                    var spName = "fn_pod_click";
                    var dataReader = _dbHelper.ExecuteScalar(spName, CommandType.StoredProcedure, dbParams);
                    ret = Convert.ToBoolean(dataReader);

                    var pod1 = _context.POD_CheckDMO_con.Where(a => a.customer_id == dto.customer_id).ToList();
                    foreach (var ww in pod1)
                    {
                        var pod2 = _context.POD_CheckDMO_con.Where(a => a.pod_id == ww.pod_id).SingleOrDefault();
                        _context.Remove(pod2);
                        _context.SaveChanges();
                    }

                }
                if (ret == true)
                {
                    dto.status = "Failed";
                    return dto;
                }


                foreach (var item in dto.ordercartlist)
                {
                    var itmqty = _context.Product_ItemDMO_con.Where(a => a.item_id == item.itemid).ToList();
                    if (item.quantity > itmqty[0].quantity)
                    {
                        dto.ret_itemname = itmqty[0].item_name;
                        dto.ret_itemqty = itmqty[0].quantity;
                        dto.status = "Failed";
                        return dto;
                    }
                }


                var checkdata = _context.Customer_OrderDMO_con.ToList();
                if (checkdata.Count != 0)
                {
                    orderid = _context.Customer_OrderDMO_con.Max(x => x.order_id);
                    orderid++;
                }
                else
                {
                    orderid = 100;
                }
                var invoiceadd = _context.Customer_Address_con.Where(a => a.user_id == dto.user_id && a.default_address == true).FirstOrDefault();

                foreach (var item in dto.ordercartlist)
                {
                    var item_mrp = _context.Product_ItemDMO_con.Where(a => a.item_id == item.itemid).SingleOrDefault();

                    Customer_Order_ItemDMO dmo = new Customer_Order_ItemDMO();
                    dmo.order_id = orderid;
                    dmo.product_id = item.productid;
                    dmo.item_id = item.itemid;
                    dmo.quantity = item.quantity;
                    dmo.selling_price = item.selling_price;
                    dmo.mrp = item_mrp.mrp;
                    dmo.status_id = 1;
                    dmo.order_accept_status = "Pending";
                    _context.Add(dmo);
                    _context.SaveChanges();



                    Order_Invoice_AddressDMO invoice = new Order_Invoice_AddressDMO();
                    invoice.order_id = orderid;
                    invoice.user_id = dto.user_id;
                    invoice.order_item_id = dmo.order_item_id;
                    invoice.customer_id = dto.customer_id;
                    invoice.customer_name = invoiceadd.customer_name;
                    invoice.address_line_1 = invoiceadd.address_line_1;
                    invoice.address_line_2 = invoiceadd.address_line_2;
                    invoice.city = invoiceadd.city;
                    invoice.country_id = invoiceadd.country_id;
                    invoice.state_id = invoiceadd.state_id;
                    invoice.pincode = invoiceadd.pincode;
                    invoice.land_mark = invoiceadd.land_mark;
                    invoice.mobile = invoiceadd.mobile;
                    invoice.email_id = invoiceadd.email_id;
                    _context.Add(invoice);
                    _context.SaveChanges();




                }
                _context.SaveChanges();

                var Totalamount = (from a in _context.Customer_Order_ItemDMO_con
                                   where a.order_id == orderid
                                   select a).Sum(e => e.selling_price);
                var billinadd = _context.Customer_Shipping_Address_DMO_con.Where(a => a.user_id == dto.user_id && a.default_address == true).FirstOrDefault();

                dto.customer_name = billinadd.name;
                dto.email = billinadd.email;
                dto.mobile = billinadd.mobile;

                Customer_OrderDMO co = new Customer_OrderDMO();
                co.order_id = orderid;
                co.payment_method_id = 1;
                co.set_paid = true;
                co.status_id = 1;
                co.user_id = dto.user_id;
                co.customer_id = dto.customer_id;
                co.email = customer.email;
                co.phone_number = customer.mobile;
                co.billing_address_id = billinadd.shippingaddress_id;
                co.is_gst_available = true;
                co.delivery_charge = 0;
                co.gross_amount = dto.gross_amount;
                co.discount_id = 1;
                co.tax_calculate_id = 1;
                co.payable_amount = dto.payable_amount;
                co.total_order_amount = dto.total_order_amount;
                co.discount_amount = 0;
                co.tax_amount = 0;
                co.payment_status = "Pending";
                co.order_date = indianTime;
                _context.Add(co);
                _context.SaveChanges();

                Order_Shipping_AddressDMO ord = new Order_Shipping_AddressDMO();
                ord.order_id = orderid;
                ord.user_id = dto.user_id;
                ord.customer_id = dto.customer_id;
                ord.customer_name = billinadd.name;
                ord.address_line_1 = billinadd.address_line_1;
                ord.address_line_2 = billinadd.address_line_2;
                ord.city = billinadd.city;
                ord.country_id = billinadd.country_id;
                ord.state_id = billinadd.state_id;
                ord.pincode = billinadd.pincode;
                ord.land_mark = billinadd.land_mark;
                ord.mobile = billinadd.mobile;
                ord.email_id = billinadd.email;
                _context.Add(ord);
                var ss = _context.SaveChanges();


                if (ss > 0)
                {
                    var resultlist = _context.Customer_Order_ItemDMO_con.Where(a => a.order_id == orderid).ToList();
                    foreach (var item in resultlist)
                    {
                        var res = _context.Customer_Order_ItemDMO_con.Where(a => a.order_id == orderid && a.product_id == item.product_id && a.item_id == item.item_id).SingleOrDefault();

                        res.status_id = 2;
                        _context.Update(res);

                        var crt = _context.Cart_List_con.Where(a => a.product_id == item.product_id && a.customer_id == dto.customer_id && a.userid == dto.user_id && a.item_id == item.item_id).SingleOrDefault();
                        if (crt != null)
                        {
                            _context.Remove(crt);
                        }

                        var prdvariuant = _context.Product_ItemDMO_con.Where(a => a.product_id == item.product_id && a.item_id == item.item_id).SingleOrDefault();
                        prdvariuant.quantity = prdvariuant.quantity - item.quantity;

                        _context.Update(prdvariuant);

                    }
                    var sss = _context.SaveChanges();
                    if (sss > 0)
                    {
                        dto.status_flg = true;
                    }
                    else
                    {
                        dto.status_flg = false;
                    }
                }
                else
                {
                    dto.status_flg = false;
                }
            }
            catch (Exception ex)
            {

                _error.errorlog_add(ex, dto.user_id, methodname, dto.ipAddress, dto.apitype, page_form, "Linqe", dto.inputvalue);
            }
            finally
            {
                _error.audit_log_txr(dto.user_id, methodname, page_form);
            }
            return dto;

        }
        public CartCheckPlaceOrderDTO paymentsave(CartCheckPlaceOrderDTO dto)
        {
            var page_form = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
            string methodname = "CartCheckPlaceOrder_Repository/paymentsave";
            TimeZoneInfo INDIAN_ZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            DateTime indianTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, INDIAN_ZONE);
            IDbHelper _dbHelper = new NpgsqlHelper(conn.ConnectionString);
            var Params = new DbParameter[] { };
            long orderid = 0;

            try
            {
                _error.audit_log_txr(dto.user_id, methodname, page_form);
                var customer = _context.Customer_Profile_con.Where(a => a.user_id == dto.user_id).SingleOrDefault();
                dto.customer_id = customer.customer_id;
                // Check And Update Quantity
                var dddd = DateTime.Now.AddMinutes(-2);
                var resultqty = _context.Payment_Order_QtyDMO_con.Where(a => a.order_on <= dddd).ToList();
                if (resultqty.Count > 0)
                {
                    foreach (var item in resultqty)
                    {
                        var dbParams2 = new DbParameter[]
                      {
                        DbHelper.CreateParameter("in_payment_order_id", item.payment_order_id),
                        DbHelper.CreateParameter("in_item_id", item.item_id),
                        DbHelper.CreateParameter("in_quantity", item.quantity),
                        DbHelper.CreateParameter("in_order_on", item.order_on)
                      };
                        Params = dbParams2;
                        var spName = "call buy_quantity_update(:in_payment_order_id, :in_item_id,:in_quantity,:in_order_on)";
                        status = _dbHelper.ExecuteNonQuery(spName, CommandType.Text, dbParams2);
                    }
                }


                if (dto.payment_transaction_id != "" && dto.payment_transaction_id != null && dto.payment_transaction_id != "0")
                {
                    var checkdata = _context.Customer_OrderDMO_con.ToList();
                    if (checkdata.Count != 0)
                    {
                        orderid = _context.Customer_OrderDMO_con.Max(x => x.order_id);
                        orderid++;
                    }
                    else
                    {
                        orderid = 100;
                    }

                    var billinadd = _context.Customer_Shipping_Address_DMO_con.Where(a => a.user_id == dto.user_id && a.default_address == true).FirstOrDefault();

                    Customer_OrderDMO co = new Customer_OrderDMO();
                    co.payment_method_id = 2;
                    co.order_id = orderid;
                    co.set_paid = true;
                    co.status_id = 1;
                    co.user_id = dto.user_id;
                    co.customer_id = dto.customer_id;
                    co.email = billinadd.email;
                    co.phone_number = billinadd.mobile;
                    co.billing_address_id = billinadd.shippingaddress_id;
                    co.is_gst_available = true;
                    co.delivery_charge = 0;
                    co.gross_amount = dto.gross_amount;
                    co.discount_id = 1;
                    co.tax_calculate_id = 1;
                    co.gst_number = "1234";
                    co.payable_amount = dto.payable_amount;
                    co.total_order_amount = dto.total_order_amount;
                    co.discount_amount = 0;
                    co.tax_amount = 0;
                    co.payment_order_id = dto.payment_order_id;
                    co.transaction_id = dto.payment_transaction_id;
                    co.payment_status = "Success";
                    co.payment_received_on = indianTime;
                    co.order_date = indianTime;
                    _context.Add(co);
                    _context.SaveChanges();

                    Order_Shipping_AddressDMO ord = new Order_Shipping_AddressDMO();
                    ord.order_id = orderid;
                    ord.user_id = dto.user_id;
                    ord.customer_id = dto.customer_id;
                    ord.customer_name = billinadd.name;
                    ord.address_line_1 = billinadd.address_line_1;
                    ord.address_line_2 = billinadd.address_line_2;
                    ord.city = billinadd.city;
                    ord.country_id = billinadd.country_id;
                    ord.state_id = billinadd.state_id;
                    ord.pincode = billinadd.pincode;
                    ord.land_mark = billinadd.land_mark;
                    ord.mobile = billinadd.mobile;
                    ord.email_id = billinadd.email;
                    _context.Add(ord);
                    var ss = _context.SaveChanges();

                    var invoiceadd = _context.Customer_Address_con.Where(a => a.user_id == dto.user_id && a.default_address == true).FirstOrDefault();

                   


                    foreach (var item in dto.ordercartlist)
                    {
                        var item_mrp = _context.Product_ItemDMO_con.Where(a => a.item_id == item.itemid).SingleOrDefault();

                        Customer_Order_ItemDMO dmo = new Customer_Order_ItemDMO();
                        dmo.order_id = orderid;
                        dmo.product_id = item.productid;
                        dmo.item_id = item.itemid;
                        dmo.quantity = item.quantity;
                        dmo.selling_price = item.selling_price;
                        dmo.mrp = item_mrp.mrp;
                        dmo.status_id = 1;
                        dmo.order_accept_status = "Pending";
                        _context.Add(dmo);
                        _context.SaveChanges();

                        Order_Item_txnDMO ddm = new Order_Item_txnDMO();
                        ddm.order_id = orderid;
                        ddm.order_item_id = dmo.order_item_id;
                        ddm.order_txn_status_id = 1;
                        ddm.status_details = "";
                        ddm.is_activity_success = false;
                        ddm.created_by = dto.user_id;
                        ddm.created_on = indianTime;
                        _context.Add(ddm);
                        _context.SaveChanges();

                        Order_Invoice_AddressDMO invoice = new Order_Invoice_AddressDMO();
                        invoice.order_id = orderid;
                        invoice.user_id = dto.user_id;
                        invoice.order_item_id = dmo.order_item_id;
                        invoice.customer_id = dto.customer_id;
                        invoice.customer_name = invoiceadd.customer_name;
                        invoice.address_line_1 = invoiceadd.address_line_1;
                        invoice.address_line_2 = invoiceadd.address_line_2;
                        invoice.city = invoiceadd.city;
                        invoice.country_id = invoiceadd.country_id;
                        invoice.state_id = invoiceadd.state_id;
                        invoice.pincode = invoiceadd.pincode;
                        invoice.land_mark = invoiceadd.land_mark;
                        invoice.mobile = invoiceadd.mobile;
                        invoice.email_id = invoiceadd.email_id;
                        _context.Add(invoice);
                        _context.SaveChanges();

                    }

                    //var result = _context.Customer_OrderDMO_con.Where(a => a.order_id == dto.order_id && a.user_id == dto.user_id).SingleOrDefault();
                    //result.payment_order_id = dto.payment_order_id;
                    //result.transaction_id = dto.payment_transaction_id;
                    //result.status_id = 1;
                    //result.payment_status = "Success";
                    //result.payment_received_on = indianTime;
                    //_context.Update(result);

                    var paymentresult = _context.Payment_TransactionDMO_con.Where(a => a.payment_order_id == dto.payment_order_id && a.customer_id == dto.customer_id && a.user_id == dto.user_id).SingleOrDefault();

                    paymentresult.payment_transaction_id = dto.payment_transaction_id;
                    paymentresult.payment_status = "Success";
                    paymentresult.order_id = orderid;
                    _context.Update(paymentresult);

                    Online_Payment_Transaction_HistoryDMO ptt = new Online_Payment_Transaction_HistoryDMO();
                    ptt.online_payment_id = paymentresult.online_payment_id;
                    ptt.payment_transaction_id = dto.payment_transaction_id;
                    ptt.payment_txn_date_time = indianTime;
                    ptt.payment_txn_status = "Success";
                    ptt.payment_txn_log_details = "";
                    ptt.is_txn_success = true;
                    _context.Add(ptt);
                    _context.SaveChanges();

                    var resultlist = _context.Customer_Order_ItemDMO_con.Where(a => a.order_id == paymentresult.order_id).ToList();
                    foreach (var item in resultlist)
                    {
                        var res = _context.Customer_Order_ItemDMO_con.Where(a => a.order_id == paymentresult.order_id && a.product_id == item.product_id && a.item_id == item.item_id).SingleOrDefault();

                        res.status_id = 2;
                        _context.Update(res);



                        var crt = _context.Cart_List_con.Where(a => a.product_id == item.product_id && a.customer_id == dto.customer_id && a.userid == dto.user_id && a.item_id == item.item_id).SingleOrDefault();
                        if (crt != null)
                        {
                            _context.Remove(crt);
                        }

                        //var prdvariuant = _context.Product_ItemDMO_con.Where(a => a.product_id == item.product_id && a.item_id == item.item_id).SingleOrDefault();
                        //prdvariuant.quantity = prdvariuant.quantity - item.quantity;
                        //_context.Update(prdvariuant);

                    }
                    var sss = _context.SaveChanges();
                    if (sss > 0)
                    {
                        var resultqty1 = _context.Payment_Order_QtyDMO_con.ToList();
                        foreach (var item in resultqty1)
                        {
                            if (item.payment_order_id == dto.payment_order_id)
                            {
                                var result = _context.Payment_Order_QtyDMO_con.Where(a => a.payment_order_id == dto.payment_order_id && a.item_id == item.item_id).SingleOrDefault();
                                _context.Remove(result);
                                _context.SaveChanges();
                            }
                        }

                        dto.status_flg = true;
                    }
                    else
                    {
                        dto.status_flg = false;
                    }
                }
                else
                {
                    var paymentresult = _context.Payment_TransactionDMO_con.Where(a => a.payment_order_id == dto.payment_order_id && a.customer_id == dto.customer_id && a.user_id == dto.user_id).SingleOrDefault();
                    paymentresult.payment_transaction_id = dto.payment_transaction_id;
                    paymentresult.payment_status = "Failed";
                    paymentresult.order_id = orderid;
                    _context.Update(paymentresult);

                    Online_Payment_Transaction_HistoryDMO ptt = new Online_Payment_Transaction_HistoryDMO();
                    ptt.online_payment_id = paymentresult.online_payment_id;
                    ptt.payment_transaction_id = dto.payment_transaction_id;
                    ptt.payment_txn_date_time = indianTime;
                    ptt.payment_txn_status = "Failed";
                    ptt.payment_txn_log_details = "";
                    ptt.is_txn_success = false;
                    _context.Add(ptt);
                    var sss = _context.SaveChanges();
                    if (sss > 0)
                    {
                        dto.status_flg = true;
                    }
                    else
                    {
                        dto.status_flg = false;
                    }
                }
            }

            catch (Exception ex)
            {

                _error.errorlog_add(ex, dto.user_id, methodname, dto.ipAddress, dto.apitype, page_form, "Linqe", dto.inputvalue);
            }
            finally
            {
                _error.audit_log_txr(dto.user_id, methodname, page_form);
            }
            return dto;
        }


        // direct Checkout
        public CartCheckPlaceOrderDTO Direct_CheckOut_online(CartCheckPlaceOrderDTO dto)
        {
            var page_form = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
            string methodname = "CartCheckPlaceOrder_Service/Direct_CheckOut_online";
            var Params = new DbParameter[] { };
            TimeZoneInfo INDIAN_ZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            DateTime indianTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, INDIAN_ZONE);
            IDbHelper _dbHelper = new NpgsqlHelper(conn.ConnectionString);
            try
            {
                // Check And Update Quantity
                var dddd=DateTime.Now.AddMinutes(-2);
                var resultqty = _context.Payment_Order_QtyDMO_con.Where(a => a.order_on <= dddd).ToList();
                if (resultqty.Count > 0)
                {
                    foreach (var item in resultqty)
                    {
                        var dbParams2 = new DbParameter[]
                  {
                    DbHelper.CreateParameter("in_payment_order_id", item.payment_order_id),
                    DbHelper.CreateParameter("in_item_id", item.item_id),
                    DbHelper.CreateParameter("in_quantity", item.quantity),
                    DbHelper.CreateParameter("in_order_on", item.order_on)
                  };
                        Params = dbParams2;
                        var spName = "call buy_quantity_update(:in_payment_order_id, :in_item_id,:in_quantity,:in_order_on)";
                        status = _dbHelper.ExecuteNonQuery(spName, CommandType.Text, dbParams2);
                    }
                }


                foreach (var item in dto.ordercartlist)
                {
                    var itmqty = _context.Product_ItemDMO_con.Where(a => a.item_id == item.itemid).ToList();
                    if (item.quantity > itmqty[0].quantity)
                    {
                        dto.ret_itemname = itmqty[0].item_name;
                        dto.ret_itemqty = itmqty[0].quantity;
                        dto.status = "Failed";
                        return dto;
                    }
                }

                var customer = _context.Customer_Profile_con.Where(a => a.user_id == dto.user_id).SingleOrDefault();
                dto.customer_id = customer.customer_id;

                var billinadd = _context.Customer_Shipping_Address_DMO_con.Where(a => a.user_id == dto.user_id && a.default_address == true).FirstOrDefault();


                dto.customer_name = billinadd.name;
                dto.email = billinadd.email;
                dto.mobile = billinadd.mobile;
                Dictionary<String, object> transfersnotes = new Dictionary<String, object>();

                transfersnotes.Add("notes_1", billinadd.name);
                transfersnotes.Add("notes_2", billinadd.address_line_1);
                transfersnotes.Add("notes_3", billinadd.address_line_1);
                transfersnotes.Add("notes_4", billinadd.address_line_2);
                transfersnotes.Add("notes_5", billinadd.city);
                transfersnotes.Add("notes_6", billinadd.pincode.ToString());
                dto.inputvalue = Newtonsoft.Json.JsonConvert.SerializeObject(transfersnotes);
                var amt = dto.payable_amount;
                Dictionary<string, object> input = new Dictionary<string, object>();

                input.Add("amount", amt * 100);
                input.Add("currency", "INR");
                input.Add("payment_capture", 1);
                input.Add("notes", transfersnotes);

                RazorpayClient client = new RazorpayClient(key, secret);
                Razorpay.Api.Order order = client.Order.Create(input);
                dto.payment_orderid = order["id"].ToString();

                foreach (var item in dto.ordercartlist)
                {
                    Payment_Order_QtyDMO dmo1 = new Payment_Order_QtyDMO();
                    dmo1.item_id = item.itemid;
                    dmo1.payment_order_id = dto.payment_orderid;
                    dmo1.quantity = item.quantity;
                    dmo1.order_on = DateTime.Now;
                    _context.Add(dmo1);

                    var prdvariuant = _context.Product_ItemDMO_con.Where(a => a.product_id == item.productid && a.item_id == item.itemid).SingleOrDefault();
                    prdvariuant.quantity = prdvariuant.quantity - item.quantity;
                    _context.Update(prdvariuant);

                }


                Online_Payment_TransactionDMO pt = new Online_Payment_TransactionDMO();
                pt.customer_id = dto.customer_id;
                pt.user_id = dto.user_id;
                pt.payment_order_id = dto.payment_orderid;
                pt.amount = dto.payable_amount;
                pt.mode_of_payment = "Online";
                pt.gateway_name = "razorpay";
                pt.payment_status = "Pending";
                pt.payment_transaction_id = "";
                pt.payment_date_time = indianTime;
                _context.Add(pt);

                var ss1 = _context.SaveChanges();
                if (ss1 > 0)
                {
                    dto.status_flg = true;
                }

                else
                {
                    dto.status_flg = false;
                }


            }
            catch (Exception ex)
            {

                _error.errorlog_add(ex, dto.user_id, methodname, dto.ipAddress, dto.apitype, page_form, "Linqe", dto.inputvalue);
            }
            finally
            {
                _error.audit_log_txr(dto.user_id, methodname, page_form);
            }
            return dto;

        }
        public CartCheckPlaceOrderDTO Direct_CheckOut_POD(CartCheckPlaceOrderDTO dto)
        {
            var page_form = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
            var Params = new DbParameter[] { };
            string methodname = "CartCheckPlaceOrder_Repository/CheckOut_POD";
            IDbHelper _dbHelper = new NpgsqlHelper(conn.ConnectionString);
            bool ret = false;
            try
            {
                _error.audit_log_txr(dto.user_id, methodname, page_form);

                // Check And Update Quantity
                var dddd = DateTime.Now.AddMinutes(-2);
                var resultqty = _context.Payment_Order_QtyDMO_con.Where(a => a.order_on <= dddd).ToList();
                if (resultqty.Count > 0)
                {
                    foreach (var item in resultqty)
                    {
                        var dbParams2 = new DbParameter[]
                  {
                    DbHelper.CreateParameter("in_payment_order_id", item.payment_order_id),
                    DbHelper.CreateParameter("in_item_id", item.item_id),
                    DbHelper.CreateParameter("in_quantity", item.quantity),
                    DbHelper.CreateParameter("in_order_on", item.order_on)
                  };
                        Params = dbParams2;
                        var spName = "call buy_quantity_update(:in_payment_order_id, :in_item_id,:in_quantity,:in_order_on)";
                        status = _dbHelper.ExecuteNonQuery(spName, CommandType.Text, dbParams2);
                    }
                }



                var customer = _context.Customer_Profile_con.Where(a => a.user_id == dto.user_id).SingleOrDefault();
                dto.customer_id = customer.customer_id;
                TimeZoneInfo INDIAN_ZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                DateTime indianTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, INDIAN_ZONE);
                var crtqq = _context.Direct_CheckoutDMO_con.Where(a => a.customer_id == dto.customer_id && a.user_id == dto.user_id).SingleOrDefault();
                if (crtqq == null)
                {
                    return dto;
                }
                var pod = _context.POD_CheckDMO_con.Where(a => a.customer_id == dto.customer_id).ToList();
                if (pod.Count == 0)
                {
                    POD_CheckDMO dm = new POD_CheckDMO();
                    dm.customer_id = dto.customer_id;
                    dm.pod_time = DateTime.Now;
                    _context.Add(dm);
                    _context.SaveChanges();
                }
                else if (pod.Count > 0)
                {
                  
                    var dbParams = new DbParameter[]
                        {
                    DbHelper.CreateParameter("in_customer_id", dto.customer_id)
                        };
                    var spName = "fn_pod_click";
                    var dataReader = _dbHelper.ExecuteScalar(spName, CommandType.StoredProcedure, dbParams);
                    ret = Convert.ToBoolean(dataReader);

                    var pod1 = _context.POD_CheckDMO_con.Where(a => a.customer_id == dto.customer_id).ToList();
                    foreach (var ww in pod1)
                    {
                        var pod2 = _context.POD_CheckDMO_con.Where(a => a.pod_id == ww.pod_id).SingleOrDefault();
                        _context.Remove(pod2);
                        _context.SaveChanges();
                    }

                }
                if (ret == true)
                {
                    dto.status = "Failed";
                    return dto;
                }

                long orderid = 0;
                foreach (var item in dto.ordercartlist)
                {
                    var itmqty = _context.Product_ItemDMO_con.Where(a => a.item_id == item.itemid).ToList();
                    if (item.quantity > itmqty[0].quantity)
                    {
                        dto.ret_itemname = itmqty[0].item_name;
                        dto.ret_itemqty = itmqty[0].quantity;
                        dto.status = "Failed";
                        return dto;
                    }
                }

                var checkdata = _context.Customer_OrderDMO_con.ToList();
                if (checkdata.Count != 0)
                {
                    orderid = _context.Customer_OrderDMO_con.Max(x => x.order_id);
                    orderid++;
                }
                else
                {
                    orderid = 100;
                }
                var invoiceadd = _context.Customer_Address_con.Where(a => a.user_id == dto.user_id && a.default_address == true).FirstOrDefault();

                foreach (var item in dto.ordercartlist)
                {
                    var item_mrp = _context.Product_ItemDMO_con.Where(a => a.item_id == item.itemid).SingleOrDefault();
                    Customer_Order_ItemDMO dmo = new Customer_Order_ItemDMO();
                    dmo.order_id = orderid;
                    dmo.product_id = item.productid;
                    dmo.item_id = item.itemid;
                    dmo.quantity = item.quantity;
                    dmo.selling_price = item.selling_price;
                    dmo.mrp = item_mrp.mrp;
                    dmo.status_id = 1;
                    dmo.order_accept_status = "Pending";
                    _context.Add(dmo);
                    _context.SaveChanges();

                    Order_Invoice_AddressDMO invoice = new Order_Invoice_AddressDMO();
                    invoice.order_id = orderid;
                    invoice.user_id = dto.user_id;
                    invoice.order_item_id = dmo.order_item_id;
                    invoice.customer_id = dto.customer_id;
                    invoice.customer_name = invoiceadd.customer_name;
                    invoice.address_line_1 = invoiceadd.address_line_1;
                    invoice.address_line_2 = invoiceadd.address_line_2;
                    invoice.city = invoiceadd.city;
                    invoice.country_id = invoiceadd.country_id;
                    invoice.state_id = invoiceadd.state_id;
                    invoice.pincode = invoiceadd.pincode;
                    invoice.land_mark = invoiceadd.land_mark;
                    invoice.mobile = invoiceadd.mobile;
                    invoice.email_id = invoiceadd.email_id;
                    _context.Add(invoice);
                    _context.SaveChanges();

                }
                _context.SaveChanges();

                var Totalamount = (from a in _context.Customer_Order_ItemDMO_con
                                   where a.order_id == orderid
                                   select a).Sum(e => e.selling_price);
                var billinadd = _context.Customer_Shipping_Address_DMO_con.Where(a => a.user_id == dto.user_id && a.default_address == true).FirstOrDefault();

                dto.customer_name = billinadd.name;
                dto.email = billinadd.email;
                dto.mobile = billinadd.mobile;

                Customer_OrderDMO co = new Customer_OrderDMO();
                co.order_id = orderid;
                co.payment_method_id = 1;
                co.set_paid = true;
                co.status_id = 1;
                co.user_id = dto.user_id;
                co.customer_id = dto.customer_id;
                co.email = customer.email;
                co.phone_number = customer.mobile;
                co.billing_address_id = billinadd.shippingaddress_id;
                co.is_gst_available = true;
                co.delivery_charge = 0;
                co.gross_amount = dto.gross_amount;
                co.discount_amount = 0;
                co.tax_amount = 0;
                co.discount_id = 1;
                co.tax_calculate_id = 1;
                co.payable_amount = dto.payable_amount;
                co.total_order_amount = dto.total_order_amount;
                co.gst_number = "1234";
                co.payment_status = "Pending";
                co.order_date = indianTime;
                _context.Add(co);
                _context.SaveChanges();

                Order_Shipping_AddressDMO ord = new Order_Shipping_AddressDMO();
                ord.order_id = orderid;
                ord.user_id = dto.user_id;
                ord.customer_id = dto.customer_id;
                ord.customer_name = billinadd.name;
                ord.address_line_1 = billinadd.address_line_1;
                ord.address_line_2 = billinadd.address_line_2;
                ord.city = billinadd.city;
                ord.country_id = billinadd.country_id;
                ord.state_id = billinadd.state_id;
                ord.pincode = billinadd.pincode;
                ord.land_mark = billinadd.land_mark;
                ord.mobile = billinadd.mobile;
                ord.email_id = billinadd.email;
                _context.Add(ord);
                var ss = _context.SaveChanges();

              

               

                if (ss > 0)
                {
                    var resultlist = _context.Customer_Order_ItemDMO_con.Where(a => a.order_id == orderid).ToList();
                    foreach (var item in resultlist)
                    {
                        var res = _context.Customer_Order_ItemDMO_con.Where(a => a.order_id == orderid && a.product_id == item.product_id && a.item_id == item.item_id).SingleOrDefault();

                        res.status_id = 2;
                        _context.Update(res);

                        var crt = _context.Cart_List_con.Where(a => a.product_id == item.product_id && a.customer_id == dto.customer_id && a.userid == dto.user_id && a.item_id == item.item_id).SingleOrDefault();
                        if (crt != null)
                        {
                            _context.Remove(crt);
                        }

                        var prdvariuant = _context.Product_ItemDMO_con.Where(a => a.product_id == item.product_id && a.item_id == item.item_id).SingleOrDefault();
                        prdvariuant.quantity = prdvariuant.quantity - item.quantity;

                        _context.Update(prdvariuant);

                    }
                    var sss = _context.SaveChanges();
                    if (sss > 0)
                    {
                        dto.status_flg = true;
                    }
                    else
                    {
                        dto.status_flg = false;
                    }
                }
                else
                {
                    dto.status_flg = false;
                }
            }
            catch (Exception ex)
            {

                _error.errorlog_add(ex, dto.user_id, methodname, dto.ipAddress, dto.apitype, page_form, "Linqe", dto.inputvalue);
            }
            finally
            {
                _error.audit_log_txr(dto.user_id, methodname, page_form);
            }
            return dto;

        }
        public CartCheckPlaceOrderDTO Direct_paymentsave(CartCheckPlaceOrderDTO dto)
        {
            var page_form = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
            string methodname = "CartCheckPlaceOrder_Repository/Direct_paymentsave";
            TimeZoneInfo INDIAN_ZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            DateTime indianTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, INDIAN_ZONE);
            IDbHelper _dbHelper = new NpgsqlHelper(conn.ConnectionString);
            var Params = new DbParameter[] { };
            long orderid = 0;
            try
            {
                // Check And Update Quantity
                var dddd = DateTime.Now.AddMinutes(-2);
                var resultqty = _context.Payment_Order_QtyDMO_con.Where(a => a.order_on <= dddd).ToList();
                if (resultqty.Count > 0)
                {
                    foreach (var item in resultqty)
                    {
                        var dbParams2 = new DbParameter[]
                  {
                    DbHelper.CreateParameter("in_payment_order_id", item.payment_order_id),
                    DbHelper.CreateParameter("in_item_id", item.item_id),
                    DbHelper.CreateParameter("in_quantity", item.quantity),
                    DbHelper.CreateParameter("in_order_on", item.order_on)
                  };
                        Params = dbParams2;
                        var spName = "call buy_quantity_update(:in_payment_order_id, :in_item_id,:in_quantity,:in_order_on)";
                        status = _dbHelper.ExecuteNonQuery(spName, CommandType.Text, dbParams2);
                    }
                }


                _error.audit_log_txr(dto.user_id, methodname, page_form);
                var customer = _context.Customer_Profile_con.Where(a => a.user_id == dto.user_id).SingleOrDefault();
                dto.customer_id = customer.customer_id;
                var crtqq = _context.Direct_CheckoutDMO_con.Where(a => a.customer_id == dto.customer_id && a.user_id == dto.user_id).SingleOrDefault();
                if (crtqq.quantity > 0)
                {
                    if (dto.payment_transaction_id != "" && dto.payment_transaction_id != null && dto.payment_transaction_id != "0")
                    {


                        var checkdata = _context.Customer_OrderDMO_con.ToList();
                        if (checkdata.Count != 0)
                        {
                            orderid = _context.Customer_OrderDMO_con.Max(x => x.order_id);
                            orderid++;
                        }
                        else
                        {
                            orderid = 100;
                        }

                        var billinadd = _context.Customer_Shipping_Address_DMO_con.Where(a => a.user_id == dto.user_id && a.default_address == true).FirstOrDefault();

                        Customer_OrderDMO co = new Customer_OrderDMO();
                        co.order_id = orderid;
                        co.payment_method_id = 2;
                        co.set_paid = true;
                        co.status_id = 1;
                        co.user_id = dto.user_id;
                        co.customer_id = dto.customer_id;
                        co.email = billinadd.email;
                        co.phone_number = billinadd.mobile;
                        co.billing_address_id = billinadd.shippingaddress_id;
                        co.is_gst_available = true;
                        co.delivery_charge = 0;
                        co.gross_amount = dto.gross_amount;
                        co.discount_id = 1;
                        co.tax_calculate_id = 1;
                        co.gst_number = "1234";
                        co.payable_amount = dto.payable_amount;
                        co.total_order_amount = dto.total_order_amount;
                        co.discount_amount = 0;
                        co.tax_amount = 0;
                        co.payment_order_id = dto.payment_order_id;
                        co.transaction_id = dto.payment_transaction_id;
                        co.payment_status = "Success";
                        co.payment_received_on = indianTime;
                        co.order_date = indianTime;
                        _context.Add(co);
                        _context.SaveChanges();

                        Order_Shipping_AddressDMO ord = new Order_Shipping_AddressDMO();
                        ord.order_id = orderid;
                        ord.user_id = dto.user_id;
                        ord.customer_id = dto.customer_id;
                        ord.customer_name = billinadd.name;
                        ord.address_line_1 = billinadd.address_line_1;
                        ord.address_line_2 = billinadd.address_line_2;
                        ord.city = billinadd.city;
                        ord.country_id = billinadd.country_id;
                        ord.state_id = billinadd.state_id;
                        ord.pincode = billinadd.pincode;
                        ord.land_mark = billinadd.land_mark;
                        ord.mobile = billinadd.mobile;
                        ord.email_id = billinadd.email;
                        _context.Add(ord);
                        var ss = _context.SaveChanges();


                        var invoiceadd = _context.Customer_Address_con.Where(a => a.user_id == dto.user_id && a.default_address == true).FirstOrDefault();


                        var ordid = co.order_id;
                        foreach (var item in dto.ordercartlist)
                        {
                            var item_mrp = _context.Product_ItemDMO_con.Where(a => a.item_id == item.itemid).SingleOrDefault();
                            Customer_Order_ItemDMO dmo = new Customer_Order_ItemDMO();
                            dmo.order_id = ordid;
                            dmo.product_id = item.productid;
                            dmo.item_id = item.itemid;
                            dmo.quantity = item.quantity;
                            dmo.selling_price = item.selling_price;
                            dmo.mrp = item_mrp.mrp;
                            dmo.status_id = 1;
                            dmo.order_accept_status = "Pending";
                            _context.Add(dmo);
                            _context.SaveChanges();

                            Order_Item_txnDMO ddm = new Order_Item_txnDMO();
                            ddm.order_id = ordid;
                            ddm.order_item_id = dmo.order_item_id;
                            ddm.order_txn_status_id = 1;
                            ddm.status_details = "";
                            ddm.is_activity_success = false;
                            ddm.created_by = dto.user_id;
                            ddm.created_on = indianTime;
                            _context.Add(ddm);
                            _context.SaveChanges();

                            Order_Invoice_AddressDMO invoice = new Order_Invoice_AddressDMO();
                            invoice.order_id = orderid;
                            invoice.user_id = dto.user_id;
                            invoice.order_item_id = dmo.order_item_id;
                            invoice.customer_id = dto.customer_id;
                            invoice.customer_name = invoiceadd.customer_name;
                            invoice.address_line_1 = invoiceadd.address_line_1;
                            invoice.address_line_2 = invoiceadd.address_line_2;
                            invoice.city = invoiceadd.city;
                            invoice.country_id = invoiceadd.country_id;
                            invoice.state_id = invoiceadd.state_id;
                            invoice.pincode = invoiceadd.pincode;
                            invoice.land_mark = invoiceadd.land_mark;
                            invoice.mobile = invoiceadd.mobile;
                            invoice.email_id = invoiceadd.email_id;
                            _context.Add(invoice);
                            _context.SaveChanges();

                        }

                        var paymentresult = _context.Payment_TransactionDMO_con.Where(a => a.payment_order_id == dto.payment_order_id && a.customer_id == dto.customer_id && a.user_id == dto.user_id).SingleOrDefault();

                        paymentresult.payment_transaction_id = dto.payment_transaction_id;
                        paymentresult.payment_status = "Success";
                        paymentresult.order_id = orderid;
                        _context.Update(paymentresult);

                        Online_Payment_Transaction_HistoryDMO ptt = new Online_Payment_Transaction_HistoryDMO();
                        ptt.online_payment_id = paymentresult.online_payment_id;
                        ptt.payment_transaction_id = dto.payment_transaction_id;
                        ptt.payment_txn_date_time = indianTime;
                        ptt.payment_txn_status = "Success";
                        ptt.payment_txn_log_details = "";
                        ptt.is_txn_success = true;
                        _context.Add(ptt);
                        _context.SaveChanges();

                        var resultlist = _context.Customer_Order_ItemDMO_con.Where(a => a.order_id == paymentresult.order_id).ToList();
                        foreach (var item in resultlist)
                        {
                            var res = _context.Customer_Order_ItemDMO_con.Where(a => a.order_id == paymentresult.order_id && a.product_id == item.product_id && a.item_id == item.item_id).SingleOrDefault();

                            res.status_id = 2;
                            _context.Update(res);

                            var crt = _context.Direct_CheckoutDMO_con.Where(a => a.customer_id == dto.customer_id && a.user_id == dto.user_id).SingleOrDefault();
                            if (crt != null)
                            {
                                _context.Remove(crt);
                            }

                            //var prdvariuant = _context.Product_ItemDMO_con.Where(a => a.product_id == item.product_id && a.item_id == item.item_id).SingleOrDefault();
                            //prdvariuant.quantity = prdvariuant.quantity - item.quantity;
                            //_context.Update(prdvariuant);

                        }
                        var sss = _context.SaveChanges();
                        if (sss > 0)
                        {
                            var resultqty1 = _context.Payment_Order_QtyDMO_con.ToList();
                            foreach (var item in resultqty1)
                            {
                                if (item.payment_order_id == dto.payment_order_id)
                                {
                                    var result = _context.Payment_Order_QtyDMO_con.Where(a => a.payment_order_id == dto.payment_order_id && a.item_id == item.item_id).SingleOrDefault();
                                    _context.Remove(result);
                                    _context.SaveChanges();
                                }
                            }
                            dto.status_flg = true;
                        }
                        else
                        {
                            dto.status_flg = false;
                        }
                    }
                    else
                    {
                        var paymentresult = _context.Payment_TransactionDMO_con.Where(a => a.payment_order_id == dto.payment_order_id && a.customer_id == dto.customer_id && a.user_id == dto.user_id).SingleOrDefault();

                        paymentresult.payment_transaction_id = dto.payment_transaction_id;
                        paymentresult.payment_status = "Failed";
                        paymentresult.order_id = orderid;
                        _context.Update(paymentresult);

                        Online_Payment_Transaction_HistoryDMO ptt = new Online_Payment_Transaction_HistoryDMO();
                        ptt.online_payment_id = paymentresult.online_payment_id;
                        ptt.payment_transaction_id = dto.payment_transaction_id;
                        ptt.payment_txn_date_time = indianTime;
                        ptt.payment_txn_status = "Failed";
                        ptt.payment_txn_log_details = "";
                        ptt.is_txn_success = false;
                        _context.Add(ptt);
                        var sss = _context.SaveChanges();
                        if (sss > 0)
                        {
                            dto.status_flg = true;
                        }
                        else
                        {
                            dto.status_flg = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                _error.errorlog_add(ex, dto.user_id, methodname, dto.ipAddress, dto.apitype, page_form, "Linqe", dto.inputvalue);
            }
            finally
            {
                _error.audit_log_txr(dto.user_id, methodname, page_form);
            }
            return dto;
        }
        public CartCheckPlaceOrderDTO check_item_available(CartCheckPlaceOrderDTO dto)
        {
            IDbHelper _dbHelper = new NpgsqlHelper(conn.ConnectionString);
            var Params = new DbParameter[] { };
            var page_form = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
            string methodname = "CartCheckPlaceOrder_Repository/Direct_paymentsave";
            TimeZoneInfo INDIAN_ZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            DateTime indianTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, INDIAN_ZONE);
            try
            {
                // Check And Update Quantity
                var dddd = DateTime.Now.AddMinutes(-2);
                var resultqty = _context.Payment_Order_QtyDMO_con.Where(a => a.order_on <= dddd).ToList();
                if (resultqty.Count > 0)
                {
                    foreach (var item in resultqty)
                    {
                        var dbParams2 = new DbParameter[]
                  {
                    DbHelper.CreateParameter("in_payment_order_id", item.payment_order_id),
                    DbHelper.CreateParameter("in_item_id", item.item_id),
                    DbHelper.CreateParameter("in_quantity", item.quantity),
                    DbHelper.CreateParameter("in_order_on", item.order_on)
                  };
                        Params = dbParams2;
                        var spName = "call buy_quantity_update(:in_payment_order_id, :in_item_id,:in_quantity,:in_order_on)";
                        status = _dbHelper.ExecuteNonQuery(spName, CommandType.Text, dbParams2);
                    }
                }

                foreach (var item in dto.ordercartlist)
                {
                    var itmqty = _context.Product_ItemDMO_con.Where(a => a.item_id == item.itemid).ToList();
                    if (item.quantity > itmqty[0].quantity)
                    {
                        dto.ret_itemname = itmqty[0].item_name;
                        dto.ret_itemqty = itmqty[0].quantity;
                        dto.status = "Failed";
                        return dto;
                    }
                }
            }
            catch (Exception ex)
            {

                _error.errorlog_add(ex, dto.user_id, methodname, dto.ipAddress, dto.apitype, page_form, "Linqe", dto.inputvalue);
            }
            finally
            {
                _error.audit_log_txr(dto.user_id, methodname, page_form);
            }
            return dto;
        }
    }
}
