﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Throb.Data.DbContext;
using Throb.Data.Entities;
using Throb.Repository.Interfaces;

namespace Throb.Repository.Repositories
{
    public class StudentRepository : GenericRepository<Student>, IStudentRepository
    {
        private readonly ThrobDbContext _context;

        public StudentRepository(ThrobDbContext context) : base(context)
        {
            _context = context;
        }

        //public void Add(Student student)
        // => _context.Add(student);

        //public void Delete(Student student)
        //=>_context.Remove(student);

        //public IEnumerable<Student> GetAll()
        //=>_context.Students.ToList();

        //public Student GetById(int id)
        //=> _context.Students.Find(id);

        //public void Update(Student student)
        //    =>_context.Update(student);

    }
}
