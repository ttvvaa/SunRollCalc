using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace SunRollCalc.Controllers
{
    public class CalcController : Controller
    {
        //
        // GET: /Calc/

        public ActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public ActionResult Calc(FormCollection form)
        {
            InputValues v = new InputValues();

            v.Width = Int32.Parse(form["width"]);
            v.Height = Int32.Parse(form["height"]);

            v.Mounting = form["mounting"];
            //v.Control = form["control"];

            v.Lock = form["bLock"] == "on";
            v.EmergencyOpen = form["bEmegency"] == "on";
            v.RemoteControl = form["bRemote"] == "on";

            v.IsMounting = form["bMount"] == "on";

            v.IsDelivery = form["bDelivery"] == "on";
            v.KmDelivery = Int32.Parse(form["kmDelivery"]);

            string price = Server.MapPath("~/App_Data/table_policarbon.xlsx");

            SunRollCalc calc = new SunRollCalc(price);

            List<EstimatePosition> estimate = new List<EstimatePosition>();

            EstimatePosition m;
            if (v.Mounting == "int")
                m = calc.CalculateInt(v);
            else
                m = calc.CalculateExt(v);
            estimate.Add(m);
            if (m.Price > 0)
            {

                if (v.RemoteControl)
                {
                    if (v.Mounting == "int")
                        estimate.Add(calc.CalculateGearInt(v));
                    else
                        estimate.Add(calc.CalculateGearExt(v));
                }

                if (v.IsMounting)
                    estimate.Add(calc.CalculateMounting(v));

                if (v.IsDelivery)
                    //if (v.KmDelivery > 0)
                    estimate.Add(calc.CalculateDelivery(v));
            }

            var json = new JavaScriptSerializer().Serialize(v);
            var smeta = new JavaScriptSerializer().Serialize(estimate);

            ViewBag.Json = json;
            ViewBag.Smeta = smeta;
            ViewBag.Estimates = estimate;

            //Elmah.ErrorLog.Default.Log();
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Info("smeta");
            logger.Debug("smeta2");

            return View();
        }

        [HttpPost]
        public JsonResult TestJson([System.Web.Http.FromBody]JsonTestClass c)
        {
            var test = new JsonTestClass() { Name = "test", Value = "value=" + c.Value };
            //return Json(test, JsonRequestBehavior.AllowGet);
            return Json(test);
        }


        [HttpPost]
        public JsonResult CalcPolicarbon([System.Web.Http.FromBody]InputValues v)
        {
            try
            {
                string price = Server.MapPath("~/App_Data/table_policarbon.xlsx");
                SunRollCalc calc = new SunRollCalc(price);
                List<EstimatePosition> estimate = new List<EstimatePosition>();
                EstimatePosition m;
                if (v.Mounting == "int")
                    m = calc.CalculateInt(v);
                else
                    m = calc.CalculateExt(v);
                estimate.Add(m);
                if (m.Price > 0)
                {
                    if (v.RemoteControl)
                    {
                        if (v.Mounting == "int")
                            estimate.Add(calc.CalculateGearInt(v));
                        else
                            estimate.Add(calc.CalculateGearExt(v));
                    }
                    if (v.IsMounting)
                        estimate.Add(calc.CalculateMounting(v));

                    if (v.Lock)
                        estimate.Add(new EstimatePosition("Замок с ключом:", 500));

                    if (v.IsDelivery)
                        estimate.Add(calc.CalculateDelivery(v));
                }
                double sum = estimate.Sum(i => i.Price);
                return Json(new { Estimate = estimate, Price = sum.ToString() + " руб." });
            }
            catch (Exception ex)
            {
                return Json(new { Price = -1, Error = ex.Message });
            }
        }

		[HttpPost]
		public JsonResult SendEmail(string fio, string email, string phone, string body)
		{
			try
			{
				//GMailer.GmailUsername = ConfigurationManager.AppSettings["GmailUserNameKey"];
				//GMailer.GmailPassword = ConfigurationManager.AppSettings["GmailPasswordKey"];
				//GMailer mailer = new GMailer();
				//mailer.ToEmail = "ttvvaa@gmail.com";
				//mailer.Subject = "Verify your email id";
				//mailer.Body = "Thanks for Registering your account.<br> please verify your email id by clicking the link <br> <a href='youraccount.com/verifycode=12323232'>verify</a>";
				//mailer.IsHtml = true;
				//mailer.Send();

				// TODO тут созраняем в БД
				
				GMailer.GmailUsername = ConfigurationManager.AppSettings["GmailUserNameKey"];
				GMailer.GmailPassword = ConfigurationManager.AppSettings["GmailPasswordKey"];
				GMailer mailer = new GMailer();
				mailer.ToEmail = !String.IsNullOrWhiteSpace(email) ? email : "info@sun-roll.ru";
				mailer.Subject = "Заказ рольставни";
				mailer.Body = body;
				mailer.IsHtml = true;
				mailer.Send();


				return Json(new { Status = "OK" });
			}
			catch (Exception ex)
			{
				return Json(new { Status = "ERROR" });
			}
		}
    }


    public class JsonTestClass
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }


    public class SunRollCalc
    {
        private static int COLS = 41; // кол-во значений ширины
        private static int ROWS = 18; // кол-во значений высоты

        double[,] priceInt = new double[ROWS, COLS]; // встроенный монтаж
        double[,] priceExt = new double[ROWS, COLS]; // накладной монтаж

        string[,] gearInt = new string[ROWS, COLS]; // модель автоматического привода
        string[,] gearExt = new string[ROWS, COLS]; // модель автоматического привода

        Dictionary<string, double> gearPrice;   // стоимость приводов  ( суффикс-М - с аварийным открыванием)
        Dictionary<string, string> gearColors;

        int[] widthsInt = new int[COLS];
        int[] heightsInt = new int[ROWS];

        int[] widthsExt = new int[COLS];
        int[] heightsExt = new int[ROWS];




        public SunRollCalc(string excelFile)
        {
            // инициализация стоимости приводов
            gearPrice = new Dictionary<string, double>();
            gearPrice.Add("RS10", 2548);
            gearPrice.Add("RS20", 2660);
            gearPrice.Add("RS30", 3080);
            gearPrice.Add("RS40", 3360);
            gearPrice.Add("RS50", 3500);
            gearPrice.Add("RS60", 5502);
            gearPrice.Add("RS80", 6510);

            gearPrice.Add("RS10M", 3290);
            gearPrice.Add("RS20M", 3416);
            gearPrice.Add("RS30M", 4116);
            gearPrice.Add("RS40M", 4256);
            gearPrice.Add("RS50M", 4396);
            gearPrice.Add("RS60M", 7364);
            gearPrice.Add("RS80M", 7616);



            using (var package = new ExcelPackage(new FileInfo(excelFile)))
            {
                // здесь выбираем Лист Эксель
                var ws1 = package.Workbook.Worksheets[1];
                var ws2 = package.Workbook.Worksheets[2];

                gearColors = new Dictionary<string, string>();
                gearColors.Add(ws1.Cells["B2"].Style.Fill.BackgroundColor.Rgb, "RS10");
                gearColors.Add(ws1.Cells["H8"].Style.Fill.BackgroundColor.Rgb, "RS20");
                gearColors.Add(ws1.Cells["M14"].Style.Fill.BackgroundColor.Rgb, "RS30");
                gearColors.Add(ws1.Cells["R14"].Style.Fill.BackgroundColor.Rgb, "RS40");
                gearColors.Add(ws1.Cells["Y14"].Style.Fill.BackgroundColor.Rgb, "RS50");
                gearColors.Add(ws1.Cells["AF14"].Style.Fill.BackgroundColor.Rgb, "RS60");
                gearColors.Add(ws1.Cells["AH16"].Style.Fill.BackgroundColor.Rgb, "RS80");

                for (int i = 2; i <= COLS + 1; i++)
                {
                    widthsInt[i - 2] = Int32.Parse(ws1.Cells[1, i].Value.ToString());
                    widthsExt[i - 2] = Int32.Parse(ws2.Cells[1, i].Value.ToString());
                }

                for (int j = 2; j <= ROWS + 1; j++)
                {
                    heightsInt[j - 2] = Int32.Parse(ws1.Cells[j, 1].Value.ToString());
                    heightsExt[j - 2] = Int32.Parse(ws2.Cells[j, 1].Value.ToString());
                }

                for (int i = 2; i <= ROWS + 1; i++)
                    for (int j = 2; j <= COLS + 1; j++)
                    {
                        if (ws1.Cells[i, j].Value != null)
                        {
                            priceInt[i - 2, j - 2] = Double.Parse(ws1.Cells[i, j].Value.ToString());
                            gearInt[i - 2, j - 2] = gearColors[ws1.Cells[i, j].Style.Fill.BackgroundColor.Rgb];
                        }

                        if (ws2.Cells[i, j].Value != null)
                        {
                            priceExt[i - 2, j - 2] = Double.Parse(ws2.Cells[i, j].Value.ToString());
                            gearExt[i - 2, j - 2] = gearColors[ws2.Cells[i, j].Style.Fill.BackgroundColor.Rgb];
                        }
                    }

                //ws.Dump();


                //types.Dump("types");
            }

            //heights.Dump();
            // priceExt.Dump();
        }




        public EstimatePosition CalculateInt(InputValues v)
        {
            // double price = 0;
            int h = GetHeightIndex(v.Height, heightsInt);
            if (h == -1)
                return new EstimatePosition("Неверное значение высоты: " + v.Height, 0);



            int w = Array.IndexOf(widthsInt, v.Width);
            //		
            if (w > -1)
                return new EstimatePosition("Стоимость конструкции встроенного монтажа (Ширина=" + v.Width + ", высота=" + v.Height + "): ", priceInt[h, w]);
            else
            {
                // 4.	Расчет стоимости по ширине проема по возможности сделать «плавающим». 
                int w1 = -1;
                int w2 = -1;

                for (int i = 0; i < COLS - 1; i++)
                {
                    if (widthsInt[i] >= v.Width)
                    {
                        w1 = i - 1;
                        w2 = i;
                        break;
                    }
                }
                //			
                //widthsInt.Dump();
                double price = (priceInt[h, w1] + priceInt[h, w2]) / (widthsInt[w1] + widthsInt[w2]) * v.Width;
                return new EstimatePosition("Стоимость конструкции встроенного монтажа (Ширина=" + v.Width + ", высота=" + v.Height + "): ", Math.Truncate(price));
                //			
            }


            //return (h + " " + w).ToString();
        }

        public EstimatePosition CalculateExt(InputValues v)
        {
            // double price = 0;
            int h = GetHeightIndex(v.Height, heightsExt);
            if (h == -1)
                return new EstimatePosition("Неверное значение высоты: " + v.Height, 0);



            int w = Array.IndexOf(widthsExt, v.Width);
            //		
            if (w > -1)
                return new EstimatePosition("Стоимость конструкции накладного монтажа (Ширина=" + v.Width + " Высота=" + v.Height + "): ", priceInt[h, w]);
            else
            {
                // 4.	Расчет стоимости по ширине проема по возможности сделать «плавающим». 
                int w1 = -1;
                int w2 = -1;

                for (int i = 0; i < COLS - 1; i++)
                {
                    if (widthsExt[i] >= v.Width)
                    {
                        w1 = i - 1;
                        w2 = i;
                        break;
                    }
                }
                //			
                //widthsInt.Dump();
                double price = (priceExt[h, w1] + priceExt[h, w2]) / (widthsExt[w1] + widthsExt[w2]) * v.Width;
                //return h + " w12=" + w1 + " " + w2 +  " Стоимость конструкции накладного монтажа: " + widthsExt[w1] + " " + widthsExt[w2] + " v.Width= " + v.Width + " price=" + price;
                return new EstimatePosition("Стоимость конструкции накладного монтажа (Ширина=" + v.Width + " Высота=" + v.Height + "): ", Math.Truncate(price));
                //			
            }

        }

        public EstimatePosition CalculateMounting(InputValues v)
        {
            double price = 8000;	 // TODO вынести в настройки
            double x = v.Height * v.Width / 1000000;
            double area = Math.Ceiling(x);

            if (area > 8)
                price += 1000 * (area - 8);

            return new EstimatePosition("Стоимость монтажа: ", price);
        }


        public EstimatePosition CalculateDelivery(InputValues v)
        {
            // TODO вынести в настройки (в эксель)
            return new EstimatePosition("Стоимость доставки (км от МКАД = " + v.KmDelivery + "): ", 3000 + 30 * v.KmDelivery);
        }

        //	private double CalculateGear(InputValues v, string[,] g, int[] heights, int[] widths)
        //	{
        //	   int h = GetHeightIndex(v.Height, heights);
        //	   int w = GetWidthIndex(v.Width, widths);
        //	   
        //	   return gearPrice[g[h, w]];
        //	}

        public EstimatePosition CalculateGearInt(InputValues v)
        {
            int h = GetHeightIndex(v.Height, heightsInt);
            int w = GetWidthIndex(v.Width, widthsInt);

            if (v.EmergencyOpen)
                return new EstimatePosition("Стоимость привода (встроенный монтаж с аварийным открыванием) " + gearInt[h, w] + "M:", gearPrice[gearInt[h, w] + "M"]);
            else
				return new EstimatePosition("Стоимость привода (встроенный монтаж без аварийного открывания) " + gearInt[h, w] + ":", gearPrice[gearInt[h, w]]);
        }


        public EstimatePosition CalculateGearExt(InputValues v)
        {
            int h = GetHeightIndex(v.Height, heightsExt);
            int w = GetWidthIndex(v.Width, widthsExt);
            if (v.EmergencyOpen)
                return new EstimatePosition("Стоимость привода (накладной монтаж с аварийным открыванием) " + gearExt[h, w] + "M:", gearPrice[gearExt[h, w] + "M"]);
            else
				return new EstimatePosition("Стоимость привода (накладной монтаж без аварийного открывания) " + gearExt[h, w] + ":", gearPrice[gearExt[h, w]]);
        }


        private int GetHeightIndex(int height, int[] a)
        {

            //heightsInt.Dump();
            int hIndex = -1;

            for (int i = 0; i < ROWS; i++)
                try
                {
                    if (a[i] >= height)
                    {
                        hIndex = i;
                        break;
                    }
                }

                catch
                {
                    // (height + " " + i).Dump("error");
                    hIndex = -2;
                }
            return hIndex;
        }

        private int GetWidthIndex(int width, int[] a)
        {

            //heightsInt.Dump();
            int wIndex = -1;

            for (int i = 0; i < COLS; i++)
                try
                {
                    if (a[i] >= width)
                    {
                        wIndex = i;
                        break;
                    }
                }

                catch
                {
                    // (height + " " + i).Dump("error");
                    wIndex = -2;
                }
            return wIndex;
        }
    }


    // Строка в смете
    public class EstimatePosition
    {

        public EstimatePosition(string s, double p)
        {
            Caption = s;
            Price = p;
        }

        public string Caption { get; set; }
        public double Price { get; set; }
    }


    // Расчет стоимостиInputValues рольставни из поликарбоната
    public class InputValues
    {
        // input values
        public int Width { get; set; } // Ширина проема (мм)
        public int Height { get; set; } // Высота проема (мм)


        // Тип монтажа
        public string Mounting { get; set; } // INT, EXT

        // Тип управления
        //public string Control { get; set; } // MANUAL, AUTO

        // Замок с ключом
        public bool Lock { get; set; }

        // Аварийное открытие
        public bool EmergencyOpen { get; set; }

        // Дистанционное управление
        public bool RemoteControl { get; set; }

        // Монтаж
        public bool IsMounting { get; set; }

        // Доставка
        public bool IsDelivery { get; set; }

        // км от МКАД
        public int KmDelivery { get; set; }

    }


	public class GMailer
	{
		public static string GmailUsername { get; set; }
		public static string GmailPassword { get; set; }
		public static string GmailHost { get; set; }
		public static int GmailPort { get; set; }
		public static bool GmailSSL { get; set; }

		public string ToEmail { get; set; }
		public string Subject { get; set; }
		public string Body { get; set; }
		public bool IsHtml { get; set; }

		static GMailer()
		{
			GmailHost = "smtp.gmail.com";
			GmailPort = 25; // Gmail can use ports 25, 465 & 587; but must be 25 for medium trust environment.
			GmailSSL = true;
		}

		public void Send()
		{
			SmtpClient smtp = new SmtpClient();
			smtp.Host = GmailHost;
			smtp.Port = GmailPort;
			smtp.EnableSsl = GmailSSL;
			smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
			smtp.UseDefaultCredentials = false;
			smtp.Credentials = new NetworkCredential(GmailUsername, GmailPassword);

			using (var message = new MailMessage(GmailUsername, ToEmail))
			{
				message.Bcc.Add("z.vadim2010@gmail.com;info@sun-roll.ru");
				message.Subject = Subject;
				message.Body = Body;
				message.IsBodyHtml = IsHtml;
				smtp.Send(message);
			}
		}
	}
}
