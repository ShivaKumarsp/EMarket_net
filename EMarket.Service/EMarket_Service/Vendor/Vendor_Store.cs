﻿using EMarket.BLL.Comman_Class;
using EMarket.BLL.Comman_Class.Interface;
using EMarket.BLL.Interfaces.Vendor;
using EMarket.Entities;
using EMarketDTO.Products;
using LiteX.DbHelper.Core;
using LiteX.DbHelper.Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EMarket.BLL.EMarket_Service.Vendor
{
    public class Vendor_Store : IVendor_Store
    {
        ISqlClass _sql;
        IErrorClass _error;
        bool saved = false;
        PostgreSqlContext _context;
        Db_Connection conn = new Db_Connection();
        List<string> invalue = new List<string>();
        int status = 0;
        public Vendor_Store(PostgreSqlContext context, ISqlClass sql, IErrorClass error)
        {
            _context = context;
            _sql = sql;
            _error = error;
        }

        public Vendor_StoreDTO getdata(Vendor_StoreDTO dto)
        {
            var vend = _context.Vendor_Profile_con.Where(a => a.user_id == dto.user_id).FirstOrDefault();
            dto.vendor_id = vend.vendor_id;
            var Params = new DbParameter[] { };
            var page_form = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
            string methodname = "Vendor_Store/getdata";
            IDbHelper _dbHelper = new NpgsqlHelper(conn.ConnectionString);
            try
            {
                // product type
                var dbParams = new DbParameter[]
                {
                      DbHelper.CreateParameter("in_language_id", dto.language_id),
                      DbHelper.CreateParameter("in_vendor_id", dto.vendor_id),
                                    };
                dto.procedure_name = "fn_get_vendor_store_list";
                Params = dbParams;
                dto.vendor_store_list = _sql.Get_Data(dto.procedure_name, dbParams);

                var dbParams2 = new DbParameter[]
                {
                      DbHelper.CreateParameter("pcountryid", dto.country_id),
                      DbHelper.CreateParameter("planguageid", dto.language_id),
                };
                dto.procedure_name = "fn_states";
                Params = dbParams2;
                dto.statelist = _sql.Get_Data(dto.procedure_name, dbParams2);
            }

            catch (Exception ex)
            {
                _error.errorlog(ex, dto.user_id, methodname, dto.ipAddress, dto.apitype, page_form, dto.procedure_name, Params);
            }
            return dto;
        }
        public Vendor_StoreDTO save_store(Vendor_StoreDTO dto)
        {
            List<string> ret_validation = new List<string>();
            var vend = _context.Vendor_Profile_con.Where(a => a.user_id == dto.user_id).FirstOrDefault();
            dto.vendor_id = vend.vendor_id;
            var Params = new DbParameter[] { };
            var page_form = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
            string methodname = "Vendor_Store/save_store";
            IDbHelper _dbHelper = new NpgsqlHelper(conn.ConnectionString);
            if (dto.store_id > 0)
            {
                var result = _context.Vendor_Store_DMO_con.Where(a => a.store_name == dto.store_name.Trim() && a.vendor_id != dto.vendor_id).ToList();
                if (result.Count > 0)
                {
                    dto.status = "Failed";
                    dto.message = "Store Name Already Exist";
                    return dto;
                }
            }
            else
            {
                var result = _context.Vendor_Store_DMO_con.Where(a => a.store_name == dto.store_name.Trim()).ToList();
                if (result.Count > 0)
                {
                    dto.status = "Failed";
                    dto.message = "Store Name Already Exist";
                    return dto;
                }
            }
            if (dto.store_name.Trim() == "" || dto.store_name.Trim() == null)
            {
                ret_validation.Add("Please Enter Store Name");
            }
            if (dto.store_name.Trim().Length < 5)
            {
                ret_validation.Add("Store Name Minimum 5 Character");
            }
            if (dto.store_title.Trim() == "" || dto.store_title.Trim() == null)
            {
                ret_validation.Add("Please Enter Store Title");
            }
            if (dto.store_title.Trim().Length < 5)
            {
                ret_validation.Add("Store Title Minimum 5 Character");
            }
            if (dto.store_details.Trim() == "" || dto.store_details.Trim() == null)
            {
                ret_validation.Add("Please Enter Store Details");
            }
            if (dto.store_details.Trim().Length < 5)
            {
                ret_validation.Add("Store Details Minimum 5 Character");
            }
            if (dto.pickup_location.Trim() == "" || dto.pickup_location.Trim() == null)
            {
                ret_validation.Add("Please Enter Pickup Location");
            }
            if (dto.pickup_location.Trim().Length < 5)
            {
                ret_validation.Add("Pickup Location Minimum 5 Character");
            }
            if (dto.city.Trim() == "" || dto.city.Trim() == null)
            {
                ret_validation.Add("Please Enter City");
            }
            if (dto.country_id == 0)
            {
                ret_validation.Add("Please Select Country");
            }
            if (dto.state_id == 0)
            {
                ret_validation.Add("Please Select State");
            }
            if (dto.pincode == 0)
            {
                ret_validation.Add("Please Enter Pincode");
            }
            if (dto.pincode < 6)
            {
                ret_validation.Add("Please Enter 6 Digit Pincode");
            }
            //Mukta-30-08-2022
            if (dto.contact_name == "" || dto.contact_name == null)
            {
                ret_validation.Add("Please Enter Contact Name");
            }
            if (dto.contact_email == "" || dto.contact_email == null)
            {
                ret_validation.Add("Please Enter Contact Email");
            }

            if (dto.contact_alternate_mobile == 0)
            {
                ret_validation.Add("Please Enter alternate Mobile");
            }

            String mob = @"^[6-9]{1}[0-9]{9}$";
            Regex mobile = new Regex(mob);

            if (!mobile.IsMatch(dto.contact_alternate_mobile.ToString()))
            {
                ret_validation.Add("Please Enter valid Alternate Mobile");
            }
            string name = @"^[a-zA-Z ]*$";
            Regex nam = new Regex(name);
            if (!nam.IsMatch(dto.contact_name))
            {
                ret_validation.Add("please Enter valid name");
            }
            string email = @"^[a-zA-Z0-9+_.-]+@[a-zA-Z0-9.-]+$";
            Regex mail = new Regex(email);
            if (!mail.IsMatch(dto.contact_email))
            {
                ret_validation.Add("please Enter valid Email");
            }
            if (ret_validation.Count > 0)
            {
                dto.status = "Validation";
                dto.validation_list = ret_validation.ToArray();
                return dto;
            }

            try
            {
                var dbParams = new DbParameter[]
                {
                     DbHelper.CreateParameter("in_language_id", dto.language_id),
                     DbHelper.CreateParameter("in_store_id", dto.store_id),
                     DbHelper.CreateParameter("in_vendor_id", dto.vendor_id),
                     DbHelper.CreateParameter("in_user_id", dto.user_id),
                     DbHelper.CreateParameter("in_store_name", dto.store_name.Trim()),
                     DbHelper.CreateParameter("in_store_title", dto.store_title.Trim()),
                     DbHelper.CreateParameter("in_store_details", dto.store_details.Trim()),
                     DbHelper.CreateParameter("in_store_image", dto.store_image),
                     DbHelper.CreateParameter("in_pickup_location", dto.pickup_location.Trim()),
                     DbHelper.CreateParameter("in_contact_name", dto.contact_name.Trim()),
                     DbHelper.CreateParameter("in_contact_primary_mobile", dto.contact_primary_mobile),
                     DbHelper.CreateParameter("in_contact_alternate_mobile", dto.contact_alternate_mobile),
                     DbHelper.CreateParameter("in_contact_email", dto.contact_email.Trim()),
                     DbHelper.CreateParameter("in_pincode", dto.pincode),
                     DbHelper.CreateParameter("in_country_id", dto.country_id),
                     DbHelper.CreateParameter("in_state_id", dto.state_id),
                     DbHelper.CreateParameter("in_city", dto.city.Trim())
                };
                dto.procedure_name = "call sp_add_vendor_store(:in_language_id,:in_store_id, :in_vendor_id,:in_user_id,:in_store_name,:in_store_title,:in_store_details,:in_store_image,:in_pickup_location,:in_contact_name,:in_contact_primary_mobile,:in_contact_alternate_mobile,:in_contact_email,:in_pincode,:in_country_id,:in_state_id,:in_city)";

                status = _dbHelper.ExecuteNonQuery(dto.procedure_name, CommandType.Text, dbParams);
                if (status == -1)
                {
                    if (dto.store_id > 0)
                    {
                        dto.status = "Insert";
                        dto.message = "Store Updated Successfully.";
                    }
                    else
                    {
                        dto.status = "Insert";
                        dto.message = "Store Saved Successfully.";
                    }

                }
                else
                {
                    dto.status = "Failed";
                    dto.message = "Saved Not Saved/Updated, Please Try Again',";
                }
            }
            catch (Exception ex)
            {
                _error.errorlog(ex, dto.user_id, methodname, dto.ipAddress, dto.apitype, page_form, dto.procedure_name, Params);
            }
            try
            {
                // product type
                var dbParams = new DbParameter[]
                {
                      DbHelper.CreateParameter("in_language_id", dto.language_id),
                      DbHelper.CreateParameter("in_vendor_id", dto.vendor_id),
                                    };
                dto.procedure_name = "fn_get_vendor_store_list";
                Params = dbParams;
                dto.vendor_store_list = _sql.Get_Data(dto.procedure_name, dbParams);


                var dbParams2 = new DbParameter[]
                {
                      DbHelper.CreateParameter("pcountryid", 1),
                      DbHelper.CreateParameter("planguageid", dto.language_id),
                };
                dto.procedure_name = "fn_states";
                Params = dbParams2;
                dto.statelist = _sql.Get_Data(dto.procedure_name, dbParams2);

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
        public Vendor_StoreDTO delete_store(Vendor_StoreDTO dto)
        {
            var vend = _context.Vendor_Profile_con.Where(a => a.user_id == dto.user_id).FirstOrDefault();
            dto.vendor_id = vend.vendor_id;
            var Params = new DbParameter[] { };
            var page_form = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
            string methodname = "Vendor_Store/delete_store";
            IDbHelper _dbHelper = new NpgsqlHelper(conn.ConnectionString);

            try
            {
                var result = _context.Product_ItemDMO_con.Where(a => a.store_id == dto.store_id).ToList();
                if (result.Count > 0)
                {
                    dto.status = "Failed";
                    dto.message = "Store Name Already Mapped With Product, You Can't Delete";
                    return dto;
                }

                var dbParams = new DbParameter[]
                {
                    DbHelper.CreateParameter("in_language_id", dto.language_id),
                    DbHelper.CreateParameter("in_store_id", dto.store_id),
                    DbHelper.CreateParameter("in_vendor_id", dto.vendor_id),
                    DbHelper.CreateParameter("in_user_id", dto.user_id)

                };

                dto.procedure_name = "call sp_delete_vendor_store(:in_language_id,:in_store_id, :in_vendor_id,:in_user_id)";
                status = _dbHelper.ExecuteNonQuery(dto.procedure_name, CommandType.Text, dbParams);
                if (status == -1)
                {
                    dto.status = "Delete";
                    dto.message = "Store Deleted Successfully.";

                }
                else
                {
                    dto.status = "Failed";
                    dto.message = "Saved Not Deleted, Please Try Again',";
                }
            }
            catch (Exception ex)
            {
                _error.errorlog(ex, dto.user_id, methodname, dto.ipAddress, dto.apitype, page_form, dto.procedure_name, Params);
            }
            try
            {
                // product type
                var dbParams = new DbParameter[]
                {
                      DbHelper.CreateParameter("in_language_id", dto.language_id),
                      DbHelper.CreateParameter("in_vendor_id", dto.vendor_id),
                                    };
                dto.procedure_name = "fn_get_vendor_store_list";
                Params = dbParams;
                dto.vendor_store_list = _sql.Get_Data(dto.procedure_name, dbParams);


                var dbParams2 = new DbParameter[]
                {
                      DbHelper.CreateParameter("pcountryid", 1),
                      DbHelper.CreateParameter("planguageid", dto.language_id),
                };
                dto.procedure_name = "fn_states";
                Params = dbParams2;
                dto.statelist = _sql.Get_Data(dto.procedure_name, dbParams2);

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

        // Vendor Bank Details
        public Vendor_StoreDTO get_bank_data(Vendor_StoreDTO dto)
        {
            var vend = _context.Vendor_Profile_con.Where(a => a.user_id == dto.user_id).FirstOrDefault();
            dto.vendor_id = vend.vendor_id;
            var Params = new DbParameter[] { };
            var page_form = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
            string methodname = "Vendor_Store/get_bank_data";

            try
            {
                var dbParams2 = new DbParameter[]
               {
                      DbHelper.CreateParameter("in_user_id", dto.user_id),
                      DbHelper.CreateParameter("in_vendor_id", dto.vendor_id),
                      DbHelper.CreateParameter("in_language_id", dto.language_id),
               };
                dto.procedure_name = "fn_get_vendor_bank_details";
                Params = dbParams2;
                dto.vendor_bank = _sql.Get_Data(dto.procedure_name, dbParams2);
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
        public Vendor_StoreDTO save_bank_data(Vendor_StoreDTO dto)
        {
            List<string> ret_validation = new List<string>();
            IDbHelper _dbHelper = new NpgsqlHelper(conn.ConnectionString);
            var vend = _context.Vendor_Profile_con.Where(a => a.user_id == dto.user_id).FirstOrDefault();
            dto.vendor_id = vend.vendor_id;
            var Params = new DbParameter[] { };
            var page_form = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
            string methodname = "Vendor_Store/save_bank_data";

            if (dto.account_holder_name.Trim() == null || dto.account_holder_name.Trim() == "")
            {
                ret_validation.Add("Please Enter Account Holder Name");
            }
            if (dto.account_holder_name.Trim().Length < 5)
            {
                ret_validation.Add("Account Holder Name Minimun 5 Character");
            }
            if (dto.bank_account.Trim() == null || dto.bank_account.Trim() == "")
            {
                ret_validation.Add("Please Enter Account Number");
            }
            if (dto.bank_account.Trim().Length < 5)
            {
                ret_validation.Add("Account Number Minimun 5 Character");
            }
            if (dto.ifsc_code.Trim() == null || dto.ifsc_code.Trim() == "")
            {
                ret_validation.Add("Please Enter IFSC Code");
            }
            String regex = "^[A-Z]{4}0[0-9]{6}$";

            Regex nre = new Regex(regex);
            if (!nre.IsMatch(dto.ifsc_code.ToString()))
            {
                ret_validation.Add("Please Enter Valid Valid IFSC Code");

            }

            if (dto.ifsc_code.Trim().Length < 7)
            {
                ret_validation.Add("IFSC Code Minimun 7 Character");
            }
            if (ret_validation.Count > 0)
            {
                dto.status = "Validation";
                dto.validation_list = ret_validation.ToArray();
                dto.message = "Please Enter Mandatory Fields";
                return dto;
            }

            try
            {
                var dbParams = new DbParameter[]
              {
                    DbHelper.CreateParameter("in_language_id", dto.language_id),
                    DbHelper.CreateParameter("in_vendor_bank_id", dto.vendor_bank_id),
                    DbHelper.CreateParameter("in_user_id", dto.user_id),
                    DbHelper.CreateParameter("in_vendor_id", dto.vendor_id),
                    DbHelper.CreateParameter("in_account_holder_name", dto.account_holder_name.Trim()),
                    DbHelper.CreateParameter("in_bank_account", dto.bank_account.Trim()),
                     DbHelper.CreateParameter("in_ifsc_code", dto.ifsc_code.Trim()),
                     DbHelper.CreateParameter("in_account_type", dto.account_type),
                     DbHelper.CreateParameter("in_passbook_image", dto.passbook_image)

              };

                dto.procedure_name = "call sp_save_vendor_bank_details(:in_language_id,:in_vendor_bank_id, :in_user_id,:in_vendor_id,:in_account_holder_name,:in_bank_account,:in_ifsc_code,:in_account_type,:in_passbook_image)";
                status = _dbHelper.ExecuteNonQuery(dto.procedure_name, CommandType.Text, dbParams);
                if (status == -1)
                {
                    if (dto.vendor_bank_id > 0)
                    {
                        dto.status = "Insert";
                        dto.message = "Bank Details Updated Successfully.";
                    }
                    else
                    {
                        dto.status = "Insert";
                        dto.message = "Bank Details Saved Successfully.";
                    }

                }
                else
                {
                    dto.status = "Failed";
                    dto.message = "Saved Not Saved/Updated, Please Try Again',";
                }


                var dbParams2 = new DbParameter[]
               {
                      DbHelper.CreateParameter("in_user_id", dto.user_id),
                      DbHelper.CreateParameter("in_vendor_id", dto.vendor_id),
                      DbHelper.CreateParameter("in_language_id", dto.language_id),
               };
                dto.procedure_name = "fn_get_vendor_bank_details";
                Params = dbParams2;
                dto.vendor_bank = _sql.Get_Data(dto.procedure_name, dbParams2);
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

        public Vendor_StoreDTO request_delete_account(Vendor_StoreDTO dto)
        {
            IDbHelper _dbHelper = new NpgsqlHelper(conn.ConnectionString);
            var vend = _context.Vendor_Profile_con.Where(a => a.user_id == dto.user_id).FirstOrDefault();
            dto.vendor_id = vend.vendor_id;
            var Params = new DbParameter[] { };
            var page_form = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
            string methodname = "Vendor_Store/request_delete_account";

            try
            {
                if (dto.request_status == "DeleteAccount")
                {
                    dto.request_delete = 1;
                }
                else if (dto.request_status == "CancelDelete")
                {
                    dto.request_delete = 0;
                }
                var dbParams = new DbParameter[]
              {
                    DbHelper.CreateParameter("in_language_id", dto.language_id),
                    DbHelper.CreateParameter("in_vendor_bank_id", dto.vendor_bank_id),
                    DbHelper.CreateParameter("in_user_id", dto.user_id),
                    DbHelper.CreateParameter("in_vendor_id", dto.vendor_id),
                    DbHelper.CreateParameter("in_request_delete", dto.request_delete)

              };

                dto.procedure_name = "call sp_delete_request_bank_details(:in_language_id,:in_vendor_bank_id, :in_user_id,:in_vendor_id,:in_request_delete)";
                status = _dbHelper.ExecuteNonQuery(dto.procedure_name, CommandType.Text, dbParams);
                if (status == -1)
                {
                    if (dto.vendor_bank_id > 0 && dto.request_delete == 1)
                    {
                        dto.status = "Insert";
                        dto.message = "Delete Request Accepted.";
                    }
                    else if (dto.vendor_bank_id > 0 && dto.request_delete == 0)
                    {
                        dto.status = "Insert";
                        dto.message = "Cancel Delete Request Accepted.";
                    }

                }
                else
                {
                    dto.status = "Failed";
                    dto.message = "Saved Not Saved/Updated, Please Try Again',";
                }


                var dbParams2 = new DbParameter[]
               {
                      DbHelper.CreateParameter("in_user_id", dto.user_id),
                      DbHelper.CreateParameter("in_vendor_id", dto.vendor_id),
                      DbHelper.CreateParameter("in_language_id", dto.language_id),
               };
                dto.procedure_name = "fn_get_vendor_bank_details";
                Params = dbParams2;
                dto.vendor_bank = _sql.Get_Data(dto.procedure_name, dbParams2);
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
    }
}
