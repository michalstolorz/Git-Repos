﻿using CarServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarServices.ViewModels
{
    public class ListRepairAssignViewModel
    {
        public List<Repair> Repairs { get; set; }
        public List<UsedRepairType> UsedRepairTypes { get; set; }
    }
}
