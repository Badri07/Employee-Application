using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Employee.Models;

namespace Employee.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmployeesController> _logger;
        public EmployeesController(IConfiguration configuration, ILogger<EmployeesController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // GET: Employees/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // GET: Employees/Error
        public IActionResult Error()
        {
            return View();
        }


        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeDetails employee)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                    {
                        await connection.OpenAsync();
                        using (var cmd = new NpgsqlCommand())
                        {
                            cmd.Connection = connection;
                            cmd.CommandText = "INSERT INTO Employee (EmployeeCode, EmployeeName, DateOfBirth, Gender, Department, Designation, BasicSalary) " +
                                              "VALUES (@EmployeeCode, @EmployeeName, @DateOfBirth, @Gender::bit, @Department, @Designation, @BasicSalary)";
                            cmd.Parameters.AddWithValue("@EmployeeCode", employee.EmployeeCode);
                            cmd.Parameters.AddWithValue("@EmployeeName", employee.EmployeeName);
                            cmd.Parameters.AddWithValue("@DateOfBirth", employee.DateOfBirth);
                            cmd.Parameters.AddWithValue("@Gender", employee.Gender ? 1 : 0); // Use the integer value here
                            cmd.Parameters.AddWithValue("@Department", employee.Department);
                            cmd.Parameters.AddWithValue("@Designation", employee.Designation);
                            cmd.Parameters.AddWithValue("@BasicSalary", employee.BasicSalary);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                return View(employee);
            }
            catch (Exception ex)
            {
                // Redirect to the error page
                return RedirectToAction("Error", "Employees"); // Ensure "Shared" matches your controller name
            }
        }

        // GET: Employees/Index
        public IActionResult Index()
        {
            List<EmployeeDetails> employees = new List<EmployeeDetails>();

            try
            {
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand("SELECT EmployeeCode, EmployeeName, DateOfBirth, Gender, Department, Designation, BasicSalary FROM Employee", connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var employee = new EmployeeDetails
                                {
                                    EmployeeCode = reader.GetInt32(0),
                                    EmployeeName = reader.GetString(1),
                                    DateOfBirth = reader.GetDateTime(2),
                                    Gender = reader.GetBoolean(3),
                                    Department = reader.GetString(4),
                                    Designation = reader.GetString(5),
                                    BasicSalary = reader.GetFloat(6)
                                };

                                // Calculate allowances
                                employee.DearnessAllowance = employee.BasicSalary * 0.4;
                                employee.ConveyanceAllowance = Math.Min(employee.DearnessAllowance * 0.1, 250);
                                employee.HouseRentAllowance = Math.Max(employee.BasicSalary * 0.25, 1500);

                                // Calculate PT deduction
                                double grossSalary = employee.BasicSalary + employee.DearnessAllowance + employee.ConveyanceAllowance + employee.HouseRentAllowance;
                                if (grossSalary <= 3000)
                                    employee.TotalSalary = grossSalary - 100;
                                else if (grossSalary <= 6000)
                                    employee.TotalSalary = grossSalary - 150;
                                else
                                    employee.TotalSalary = grossSalary - 200;

                                employees.Add(employee);
                            }
                        }
                    }
                }
                return View(employees);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Error", "Employees"); // Ensure "Shared" matches your controller name
            }
        }

        // GET: Employees/Edit/{id}
        public IActionResult Edit(int id)
        {
            try
            {
                EmployeeDetails employee = GetEmployeeById(id); // Implement this method to fetch employee details by ID from the database
                if (employee == null)
                {
                    return NotFound(); 
                }

                return View(employee); 
            }
            catch (Exception ex)
            {
                // Log the exception (optional)
                // Here you can add logging for the exception, e.g., ILogger or a logging service

                return RedirectToAction("Error", "Employees"); // Ensure "Shared" matches your controller name
            }
        }

        // POST: Employees/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, EmployeeDetails updatedEmployee)
        {
            try
            {
                if (id != updatedEmployee.EmployeeCode)
                {
                    return BadRequest(); // Return bad request if IDs don't match
                }

                if (!ModelState.IsValid)
                {
                    return View(updatedEmployee); 
                }

                UpdateEmployee(updatedEmployee); 
                return RedirectToAction(nameof(Index)); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee details.");
                // Redirect to the error page
                return RedirectToAction("Error", "Employees"); // Ensure "Shared" matches your controller name
            }
        }

        private EmployeeDetails GetEmployeeById(int id)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand("SELECT EmployeeCode, EmployeeName, DateOfBirth, Gender, Department, Designation, BasicSalary FROM Employee WHERE EmployeeCode = @EmployeeCode", connection))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeCode", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new EmployeeDetails
                                {
                                    EmployeeCode = reader.GetInt32(0),
                                    EmployeeName = reader.GetString(1),
                                    DateOfBirth = reader.GetDateTime(2),
                                    Gender = reader.GetBoolean(3),
                                    Department = reader.GetString(4),
                                    Designation = reader.GetString(5),
                                    BasicSalary = reader.GetFloat(6)
                                };
                            }
                            return null; // Return null if employee with given ID is not found
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception (optional)
                // Here you can add logging for the exception, e.g., ILogger or a logging service
                _logger.LogError(ex, "Error occurred while getting employee details.");
                // Redirect to the error page
                return null; // Return null or handle appropriately if an error occurs
            }
        }

        private void UpdateEmployee(EmployeeDetails employee)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand("UPDATE Employee SET EmployeeName = @EmployeeName, DateOfBirth = @DateOfBirth, Gender = @Gender::bit, Department = @Department, Designation = @Designation, BasicSalary = @BasicSalary WHERE EmployeeCode = @EmployeeCode", connection))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeCode", employee.EmployeeCode);
                        cmd.Parameters.AddWithValue("@EmployeeName", employee.EmployeeName);
                        cmd.Parameters.AddWithValue("@DateOfBirth", employee.DateOfBirth);
                        cmd.Parameters.AddWithValue("@Gender", employee.Gender ? 1 : 0);
                        cmd.Parameters.AddWithValue("@Department", employee.Department);
                        cmd.Parameters.AddWithValue("@Designation", employee.Designation);
                        cmd.Parameters.AddWithValue("@BasicSalary", employee.BasicSalary);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
