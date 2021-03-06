﻿using CarServices.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CarServices.ViewModels
{
    public class ChangeStatusViewModel
    {
        public int Id { get; set; }
        public List<RepairStatus> StatusList { get; set; }
        [Display(Name = "Status")]
        public int ChoosenStatusId { get; set; }
        public string Description { get; set; }
    }
}
