﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Throb.Data.Entities
{
    public class InstructorCourse
    {
    
            public int InstructorId { get; set; }
            public Instructor Instructor { get; set; }

            public int CourseId { get; set; }
            public Course Course { get; set; }
      

    }
}
