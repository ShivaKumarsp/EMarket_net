﻿using EMarketDTO.Admin;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMarket.BLL.Interfaces.Admin
{
   public interface IMap_Category_Specification
    {
        Map_Category_SpecificationDTO getdata(Map_Category_SpecificationDTO dto);
        Map_Category_SpecificationDTO savemappedspecification(Map_Category_SpecificationDTO dto);
        Map_Category_SpecificationDTO getspecification(Map_Category_SpecificationDTO dto);
        Map_Category_SpecificationDTO deletecatspecification(Map_Category_SpecificationDTO dto);

        Map_Category_SpecificationDTO getspecattribute(Map_Category_SpecificationDTO dto);
        Map_Category_SpecificationDTO getattributelist(Map_Category_SpecificationDTO dto);
        Map_Category_SpecificationDTO savemappedattribuename(Map_Category_SpecificationDTO dto);
        Map_Category_SpecificationDTO deletespecattribute(Map_Category_SpecificationDTO dto);
    }
}
