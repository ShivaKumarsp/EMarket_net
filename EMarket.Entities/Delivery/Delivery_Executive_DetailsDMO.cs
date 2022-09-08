﻿using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMarket.Entities.Delivery
{
    [Table("delivery_executive_details", Schema = "public")]
    public class Delivery_Executive_DetailsDMO
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long delivery_executive_id { get; set; }
        public long user_id { get; set; }
        public long hub_id { get; set; }
        public long facilitation_id { get; set; }
        public string first_name { get; set; }
        public string second_name { get; set; }
        public string email { get; set; }
        public long mobile { get; set; }
        public string address { get; set; }
        public string city { get; set; }
        public long state_id { get; set; }
        public long country_id { get; set; }
        public long pincode { get; set; }
        public DateTime? dob { get; set; }
        public long alternative_mobile { get; set; }
        public long gender_id { get; set; }
        public string image_url { get; set; }
       
        public string belongs_to { get; set; }
        public long own_vehicle { get; set; }
    }
}
