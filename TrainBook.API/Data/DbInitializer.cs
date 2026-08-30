using System.Security.Cryptography;
using System.Text;
using TrainBook.API.Models;

namespace TrainBook.API.Data
{
    public static class DbInitializer
    {
        // ใส่ข้อมูลตั้งต้นถ้าฐานข้อมูลยังว่าง
        public static void Seed(TrainBookContext db)
        {
            db.Database.EnsureCreated();
            if (db.Users.Any()) return;   // มีข้อมูลแล้ว ไม่ต้อง seed ซ้ำ

            // ---- บัญชีผู้ใช้ ----
            db.Users.AddRange(
                new User { Username = "admin", Email = "admin@trainbook.com", PasswordHash = Hash("admin123"),
                           FirstName = "ผู้ดูแล", LastName = "ระบบ", Role = "admin" },
                new User { Username = "user", Email = "user@example.com", PasswordHash = Hash("user123"),
                           FirstName = "สมชาย", LastName = "ใจดี", Role = "user" }
            );

            // ---- สถานี ----
            var bkk = new Station { StationName = "กรุงเทพฯ (หัวลำโพง)", StationCode = "BKK" };
            var cnx = new Station { StationName = "เชียงใหม่", StationCode = "CNX" };
            var plk = new Station { StationName = "พิษณุโลก", StationCode = "PLK" };
            var kkc = new Station { StationName = "ขอนแก่น", StationCode = "KKC" };
            db.Stations.AddRange(bkk, cnx, plk, kkc);

            // ---- ชั้นโดยสาร ----
            var c1  = new TicketClass { ClassName = "ชั้น 1", ClassCode = "C1" };
            var c2s = new TicketClass { ClassName = "ชั้น 2 (นั่งนอน)", ClassCode = "C2S" };
            var c2  = new TicketClass { ClassName = "ชั้น 2", ClassCode = "C2" };
            var c3  = new TicketClass { ClassName = "ชั้น 3", ClassCode = "C3" };
            db.TicketClasses.AddRange(c1, c2s, c2, c3);

            // ---- ขบวนรถ ----
            var t9  = new Train { TrainNumber = "9",  TrainName = "ด่วนพิเศษ", TrainType = "ด่วนพิเศษ" };
            var t13 = new Train { TrainNumber = "13", TrainName = "ด่วน", TrainType = "ด่วน" };
            var t51 = new Train { TrainNumber = "51", TrainName = "ธรรมดา", TrainType = "ธรรมดา" };
            var t67 = new Train { TrainNumber = "67", TrainName = "ธรรมดา", TrainType = "ธรรมดา" };
            db.Trains.AddRange(t9, t13, t51, t67);

            db.SaveChanges();

            var from = new DateTime(2020, 1, 1);
            var to = new DateTime(2030, 12, 31);

            // ---- ตารางเดินรถ (กรุงเทพฯ → เชียงใหม่) + ราคา ----
            void AddSchedule(Train train, string dep, string arr, int mins, (TicketClass cls, decimal price, int seats)[] prices)
            {
                var s = new Schedule
                {
                    Train = train, OriginStation = bkk, DestinationStation = cnx,
                    DepartureTime = TimeSpan.Parse(dep), ArrivalTime = TimeSpan.Parse(arr),
                    DurationMinutes = mins, IsActive = true
                };
                db.Schedules.Add(s);
                db.SaveChanges();
                foreach (var p in prices)
                    db.Prices.Add(new Price { Schedule = s, Class = p.cls, PriceAmount = p.price,
                                              AvailableSeats = p.seats, ValidFrom = from, ValidTo = to });
                db.SaveChanges();
            }

            AddSchedule(t9,  "18:10", "07:15", 785, new[] { (c1, 1250m, 16), (c2s, 850m, 42), (c2, 250m, 60) });
            AddSchedule(t13, "20:05", "08:40", 755, new[] { (c1, 1150m, 10), (c2s, 750m, 38), (c2, 750m, 55) });
            AddSchedule(t51, "07:30", "19:40", 730, new[] { (c3, 600m, 80) });
            AddSchedule(t67, "15:40", "03:50", 730, new[] { (c3, 600m, 80) });
        }

        // SHA256 → Base64 (ให้ตรงกับ AuthService.HashPassword)
        private static string Hash(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}
