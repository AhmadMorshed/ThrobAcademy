using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Throb.Data.Entities;

namespace Throb.Service.Interfaces
{
    public interface IStudentService
    {
        Student GetById(int id);
        IEnumerable<Student> GetAll();
        void Add(Student student);
        void Update(Student student);
        void Delete(Student student);
    }
}
