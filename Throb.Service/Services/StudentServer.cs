using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Throb.Data.Entities;
using Throb.Repository.Interfaces;
using Throb.Repository.Repositories;
using Throb.Service.Interfaces;

namespace Throb.Service.Services
{
    public class StudentServer : IStudentService
    {
        private readonly IStudentRepository _studentRepository;

        public StudentServer(IStudentRepository studentRepository)
        {
            _studentRepository = studentRepository;
        }

        public void Add(Student student)
        {
            var mappedStudent = new Student
            {

                Name = student.Name,
                Email= student.Email,
                Password= student.Password,
                Courses= student.Courses,
                


            };
            _studentRepository.Add(mappedStudent);
            
        }

        public void Delete(Student student)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Student> GetAll()
        {
            var students = _studentRepository.GetAll();
            return students;
        }

        public Student GetById(int id)
        {
            throw new NotImplementedException();
        }

        public void Update(Student student)
        {
            throw new NotImplementedException();
        }
    }
}
