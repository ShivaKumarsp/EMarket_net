﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EMarketDTO.Admin
{
   public class Customer_ListDTO
    {

        public string ipAddress { get; set; }
        public long user_id { get; set; }
        public long language_id { get; set; }
        public long vendor_id { get; set; }
        public string procedure_name { get; set; }
        public string apitype { get; set; }
        public string vendor_list { get; set; }
        public string customer_list { get; set; }
        public string vendor_store_list { get; set; }
    }
}
