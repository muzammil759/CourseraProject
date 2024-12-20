// See https://aka.ms/new-console-template for more information

using System.Data.SqlClient;
using System.Text;

namespace StudentReport
{
  
    public class StudentReport
    {
        public string StudentName { get; set; }
        public string CourseName { get; set; }
        public int CourseCredit { get; set; }
        public string InstructorName { get; set; }
        public int CourseTotalTime { get; set; }
        public DateTime CompletionDate { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Server=DESKTOP-LJC07I2\\SQLEXPRESS;Database=coursera;Trusted_Connection=True;";

            try
            {
                Console.WriteLine("Enter the minimum credit required:");
                int minCredit = int.Parse(Console.ReadLine());

                Console.WriteLine("Enter the start date (yyyy-MM-dd):");
                DateTime startDate = DateTime.Parse(Console.ReadLine());

                Console.WriteLine("Enter the end date (yyyy-MM-dd):");
                DateTime endDate = DateTime.Parse(Console.ReadLine());

                Console.WriteLine("Enter the path to directory:");
                String directoryPath = Console.ReadLine();


                List<StudentReport> dbDataList = FetchDataFromDB(connectionString, startDate, endDate);
               
                 
                
                var reportData = dbDataList
                    .GroupBy(sc => sc.StudentName)
                    .Select(g => new
                    {
                        StudentName = g.Key,
                        TotalCredit = g.Sum(sc => sc.CourseCredit),
                        Courses = g.Select(sc => new
                        {
                            sc.CourseName,
                            sc.CourseCredit,
                            sc.CourseTotalTime,
                            sc.InstructorName
                        })
                    })
                    .Where(r => r.TotalCredit > minCredit)
                    .OrderByDescending(r => r.TotalCredit)
                    .ToList();

               
                StringBuilder csvBuilder = new StringBuilder();
               
                csvBuilder.AppendLine("Student,Total Credit");
                csvBuilder.AppendLine(",Course Name,Time,Credit,Instructor");

                foreach (var record in reportData)
                {
                    csvBuilder.AppendLine($"{record.StudentName},{record.TotalCredit},,,,");

                    foreach (var course in record.Courses)
                    {
             
                        csvBuilder.AppendLine($",{course.CourseName},{course.CourseTotalTime.ToString()},{course.CourseCredit.ToString()},{course.InstructorName}");
                    }
                }

                
                string filePath = directoryPath + "report.csv";
                File.WriteAllText(filePath, csvBuilder.ToString());

                Console.WriteLine($"Report saved to {filePath}");
               
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        static List<StudentReport> FetchDataFromDB(string connectionString, DateTime startDate, DateTime endDate)
        {
            string query = @"
                SELECT 
                    s.first_name + ' ' + s.last_name AS student_name,
                    c.name AS course_name,
                    c.credit AS course_credit,
                    c.total_time AS course_total_time,
                    i.first_name + ' ' + i.last_name AS instructor_name,
                    scx.completion_date AS completion_date
                FROM 
                    students s
                INNER JOIN 
                    students_courses_xref scx ON s.pin = scx.student_pin
                INNER JOIN 
                    courses c ON scx.course_id = c.id
                INNER JOIN 
                    instructors i ON c.instructor_id = i.id
                WHERE 
                    scx.completion_date BETWEEN @StartDate AND @EndDate;
            ";

            var dbDataList = new List<StudentReport>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StartDate", startDate);
                    command.Parameters.AddWithValue("@EndDate", endDate);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dbDataList.Add(new StudentReport
                            {
                                StudentName = reader["student_name"].ToString(),
                                CourseName = reader["course_name"].ToString(),
                                CourseCredit = Convert.ToInt32(reader["course_credit"]),
                                CourseTotalTime = Convert.ToInt32(reader["course_total_time"]),
                                InstructorName = reader["instructor_name"].ToString(),
                                CompletionDate = Convert.ToDateTime(reader["completion_date"])
                            });
                        }
                    }
                }
            }

            return dbDataList;
        }
    }
}
