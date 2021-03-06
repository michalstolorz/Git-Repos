﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CarServices.Models;
using CarServices.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using CarServices.Models.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace CarServices.Controllers
{
    //[Authorize(Roles = "Mechanic, Admin")]
    public class MechanicController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICustomerRepository _customerRepository;
        private readonly ICarRepository _carRepository;
        private readonly ICarBrandRepository _carBrandRepository;
        private readonly ICarModelRepository _carModelRepository;
        private readonly ILocalDataRepository _localDataRepository;
        private readonly IEmployeesRepository _employeesRepository;
        private readonly IRepairTypeRepository _repairTypeRepository;
        private readonly IRepairStatusRepository _repairStatusRepository;
        private readonly IRepairRepository _repairRepository;
        private readonly IPartsRepository _partsRepository;
        private readonly IUsedPartsRepository _usedPartsRepository;
        private readonly IUsedRepairTypeRepository _usedRepairTypeRepository;

        public MechanicController(ICustomerRepository customerRepository, ICarRepository carRepository,
            ICarBrandRepository carBrandRepository, ICarModelRepository carModelRepository, ILocalDataRepository localDataRepository,
            IEmployeesRepository employeesRepository, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager,
            IRepairTypeRepository repairTypeRepository, IRepairRepository repairRepository, IHttpContextAccessor httpContextAccessor,
            IPartsRepository partsRepository, IUsedPartsRepository usedPartsRepository, IUsedRepairTypeRepository usedRepairTypeRepository,
            IRepairStatusRepository repairStatusRepository)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _customerRepository = customerRepository;
            _carRepository = carRepository;
            _carBrandRepository = carBrandRepository;
            _carModelRepository = carModelRepository;
            _localDataRepository = localDataRepository;
            _employeesRepository = employeesRepository;
            _repairTypeRepository = repairTypeRepository;
            _repairStatusRepository = repairStatusRepository;
            _repairRepository = repairRepository;
            _partsRepository = partsRepository;
            _usedPartsRepository = usedPartsRepository;
            _usedRepairTypeRepository = usedRepairTypeRepository;
        }

        [HttpGet]
        public IActionResult ListRepairAssign()
        {
            const int completeStatusId = 10;
            const int repairInProgressStatusId = 8;
            List<Repair> listRepairs = _repairRepository.GetAllRepair().Where(r => r.StatusId != completeStatusId).ToList(); 
            List<UsedRepairType> listUsedRepairTypes = _usedRepairTypeRepository.GetAllUsedRepairType().ToList();
            foreach (var l in listRepairs)
            {
                Car car = _carRepository.GetCar(l.CarId);
                CarModel carModel = _carModelRepository.GetCarModel(car.ModelId);
                l.Car.CarModel = carModel;
                CarBrand carBrand = _carBrandRepository.GetCarBrand(carModel.BrandId);
                l.Car.CarModel.CarBrand = carBrand;
                RepairStatus repairStatus = _repairStatusRepository.GetRepairStatus(l.StatusId ?? repairInProgressStatusId);
                l.RepairStatus = repairStatus;
                if (l.EmployeesId == null)
                    l.EmployeesId = 0;
                else
                {
                    Employees employee = _employeesRepository.GetEmployees(l.EmployeesId ?? 1);
                    l.Employees = employee;
                }
            }
            foreach (var l in listUsedRepairTypes)
            {
                RepairType repairType = _repairTypeRepository.GetRepairType(l.RepairTypeId);
                //repairType.Name += "\n";
                l.RepairType = repairType;
            }
            var repairListWaitingForAssignFirst = listRepairs.OrderBy(l => l.EmployeesId);
            ListRepairAssignViewModel model = new ListRepairAssignViewModel()
            {
                Repairs = repairListWaitingForAssignFirst.ToList(),
                UsedRepairTypes = listUsedRepairTypes
            };
            return View(model);
        }

        //[HttpPost]
        public IActionResult AssignRepair(int id)
        {
            string userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            Employees employees = _employeesRepository.GetAllEmployees().Where(u => u.UserId == userId).FirstOrDefault();

            const int repairInProgressStatusId = 8;
            Repair repair = _repairRepository.GetRepair(id);
            repair.StatusId = repairInProgressStatusId;
            repair.EmployeesId = employees.Id;

            _repairRepository.Update(repair);
            return RedirectToAction("ListRepairAssign", "Mechanic");
        }

        [HttpGet]
        public IActionResult ListRepairAssignedToMechanic()
        {
            const int completeStatusId = 10;
            const int repairInProgressStatusId = 8;
            string user = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            Employees employees = _employeesRepository.GetEmployeesByUserId(user);
            List<Repair> listRepairs = _repairRepository.GetAllRepair().Where(r => (r.EmployeesId == employees.Id) && (r.StatusId != completeStatusId)).ToList();
            List<UsedRepairType> listUsedRepairTypes = new List<UsedRepairType>();
            foreach (var l in listRepairs)
            {
                listUsedRepairTypes.AddRange(_usedRepairTypeRepository.GetUsedRepairTypeByRepairId(l.Id));
                RepairStatus repairStatus = _repairStatusRepository.GetRepairStatus(l.StatusId ?? repairInProgressStatusId);
                l.RepairStatus = repairStatus;
            }
            foreach (var l in listUsedRepairTypes)
            {
                l.Repair = listRepairs.Where(r => r.Id == l.RepairId).FirstOrDefault();
                Car car = _carRepository.GetCar(l.Repair.CarId);
                CarModel carModel = _carModelRepository.GetCarModel(car.ModelId);
                l.Repair.Car.CarModel = carModel;
                CarBrand carBrand = _carBrandRepository.GetCarBrand(carModel.BrandId);
                l.Repair.Car.CarModel.CarBrand = carBrand;
                RepairType repairType = _repairTypeRepository.GetRepairType(l.RepairTypeId);
                //repairType.Name += "\n";
                l.RepairType = repairType;
            }
            ListRepairAssignedToMechanicViewModel model = new ListRepairAssignedToMechanicViewModel()
            {
                repairs = listRepairs,
                usedRepairTypes = listUsedRepairTypes
            };
            return View(model);
        }

        [HttpGet]
        public IActionResult AddPartToRepair(int id)
        {
            List<UsedParts> usedPartslist = _usedPartsRepository.GetAllUsedParts().Where(up => up.RepairId == id).ToList();
            foreach (var u in usedPartslist)
            {
                Parts part = _partsRepository.GetParts(u.PartId);
                u.Part = part;
            }
            AddPartToRepairViewModel model = new AddPartToRepairViewModel()
            {
                AvailableParts = _partsRepository.GetAllParts().Where(p => p.Quantity > 0).ToList(),
                UsedParts = usedPartslist,
                RepairId = id
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult AddPartToRepair(AddPartToRepairViewModel model)
        {
            Parts partsCheck = _partsRepository.GetParts(model.ChoosenPartId);
            if (ModelState.IsValid)
            {
                if (partsCheck.Quantity >= model.UsedPartQuantity && model.UsedPartQuantity > 0)
                {
                    UsedParts usedParts = new UsedParts()
                    {
                        RepairId = model.RepairId,
                        PartId = model.ChoosenPartId,
                        Quantity = model.UsedPartQuantity
                    };
                    _usedPartsRepository.Add(usedParts);
                    Parts parts = _partsRepository.GetParts(model.ChoosenPartId);
                    parts.Quantity -= model.UsedPartQuantity;
                    _partsRepository.Update(parts);
                    return RedirectToAction("AddPartToRepair", "Mechanic", model.RepairId);
                }
                ModelState.AddModelError(string.Empty, "Wrong quantity of selected part. Not enough parts in the warehouse or value is negative");
            }
            List<UsedParts> usedPartslist = _usedPartsRepository.GetAllUsedParts().Where(up => up.RepairId == model.RepairId).ToList();
            foreach (var m in usedPartslist)
            {
                Parts part = _partsRepository.GetParts(m.PartId);
                m.Part = part;
            }
            model.AvailableParts = _partsRepository.GetAllParts().Where(p => p.Quantity > 0).ToList();
            model.UsedParts = usedPartslist;
            return View(model);
        }

        [HttpGet]
        public IActionResult SetRepairCost(int id)
        {
            List<UsedParts> usedPartslist = _usedPartsRepository.GetAllUsedParts().Where(up => up.RepairId == id).ToList();
            double summaryPriceForUsedParts = 0;
            foreach (var u in usedPartslist)
            {
                Parts part = _partsRepository.GetParts(u.PartId);
                u.Part = part;
                summaryPriceForUsedParts += u.Part.PartPrice * u.Quantity;
            }
            SetRepairCostViewModel model = new SetRepairCostViewModel()
            {
                UsedParts = usedPartslist,
                RepairId = id,
                CostForParts = summaryPriceForUsedParts
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult SetRepairCost(SetRepairCostViewModel model)
        {
            if (ModelState.IsValid)
            {
                Repair repair = _repairRepository.GetRepair(model.RepairId);
                repair.Cost = model.CostForParts + model.CostForWork;
                _repairRepository.Update(repair);
                return RedirectToAction("ListRepairAssignedToMechanic", "Mechanic");
            }
            List<UsedParts> usedPartslist = _usedPartsRepository.GetAllUsedParts().Where(up => up.RepairId == model.RepairId).ToList();
            foreach (var u in usedPartslist)
            {
                Parts part = _partsRepository.GetParts(u.PartId);
                u.Part = part;
            }
            model.UsedParts = usedPartslist;
            return View(model);
        }

        [HttpGet]
        public IActionResult ChangeStatus(int id)
        {
            const int completeStatusId = 10;
            ChangeStatusViewModel model = new ChangeStatusViewModel() { Id = id };
            model.StatusList = _repairStatusRepository.GetAllRepairStatus().ToList();
            RepairStatus repairStatus = _repairStatusRepository.GetRepairStatus(completeStatusId);
            model.StatusList.Remove(repairStatus);
            return View(model);
        }

        [HttpPost]
        public IActionResult ChangeStatus(ChangeStatusViewModel model)
        {
            if (ModelState.IsValid)
            {
                Repair repair = _repairRepository.GetRepair(model.Id);
                repair.StatusId = model.ChoosenStatusId;
                repair.Description += model.Description + ". ";
                _repairRepository.Update(repair);
                return RedirectToAction("ListRepairAssignedToMechanic", "Mechanic");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult AddRepairTypeToRepair(int id)
        {
            AddRepairTypeToRepairViewModel model = new AddRepairTypeToRepairViewModel()
            {
                RepairId = id,
                RepairTypeList = _repairTypeRepository.GetAllRepairType().ToList()
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult AddRepairTypeToRepair(AddRepairTypeToRepairViewModel model)
        {
            if (ModelState.IsValid) 
            {
                List<UsedRepairType> usedRepairTypes = _usedRepairTypeRepository.GetAllUsedRepairType().Where(u => (u.RepairId == model.RepairId) && (u.RepairTypeId == model.ChoosenRepairTypeId) ).ToList();
                if (usedRepairTypes.Count == 0)
                {
                    UsedRepairType usedRepairType = new UsedRepairType()
                    {
                        RepairId = model.RepairId,
                        RepairTypeId = model.ChoosenRepairTypeId
                    };
                    _usedRepairTypeRepository.Add(usedRepairType);
                    return RedirectToAction("ListRepairAssignedToMechanic", "Mechanic");

                }
                ModelState.AddModelError(string.Empty, "Repair type already added to current repair");
            }
            model.RepairTypeList = _repairTypeRepository.GetAllRepairType().ToList();
            if (model.ChoosenRepairTypeId == 0)
                ModelState.AddModelError(string.Empty, "Repair type hadn't been choosen");
            return View(model);
        }
    }
}
