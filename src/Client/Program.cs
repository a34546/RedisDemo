using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using WebApi.Controllers;
using WebApi.Data;

namespace Client
{
    class Program
    {
        /// <summary>
        /// 抢到的红包金额
        /// </summary>
        static List<int> Amounts = new List<int>();

        static async Task Main(string[] args)
        {
            try
            {
                await Home();
            }
            catch (Exception ex)
            {

                Console.WriteLine("出错了:" + ex);
                Console.WriteLine("按其他键退出程序");
                Console.ReadKey();
            }
        }

        static async Task Home()
        {
            Console.WriteLine("红包测试系统");
            Console.WriteLine("------------------------------");
            Console.WriteLine("1.发红包");
            Console.WriteLine("2.抢红包");
            Console.WriteLine("------------------------------");
            Console.WriteLine("请输入序号");
            var s = Console.ReadLine();
            if (s == "1")
            {
                await GiveRedPacket();
            }
            else if (s == "2")
            {
                Amounts = new List<int>();
                await GrabRedPacket();
            }
            else
            {
                Console.WriteLine("您输入的序号不正确");
            }
            Console.WriteLine("按Enter键返回菜单,按其他键退出程序");
            var keyInfo = Console.ReadKey();
            if (keyInfo.Key == ConsoleKey.Enter)
            {
                Console.Clear();
                await Home();
            }
        }

        /// <summary>
        /// 发红包
        /// </summary>
        private static async Task GiveRedPacket()
        {
            Console.WriteLine("请输入红包标题(默认为[恭喜发财,大吉大利]):");
            var title = Console.ReadLine();
            if (string.IsNullOrEmpty(title))
            {
                title = "恭喜发财,大吉大利";
            }
            Console.WriteLine("请输入红包金额(默认为[100元]):");
            var amountStr = Console.ReadLine();
            if (string.IsNullOrEmpty(amountStr))
            {
                amountStr = "100";
            }
            if (!decimal.TryParse(amountStr, out decimal amount))
            {
                Console.WriteLine("您输入的红包金额不对,程序结束");
                return;
            }
            Console.WriteLine("请输入抢红包的人数(默认为[10个]):");
            var countStr = Console.ReadLine();
            if (string.IsNullOrEmpty(countStr))
            {
                countStr = "10";
            }
            if (!int.TryParse(countStr, out int count))
            {
                Console.WriteLine("您输入抢红包的人数不对,程序结束");
                return;
            }
            var amountFen = Convert.ToInt32(amount * 100);
            using var client = new HttpClient();
            var input = new RedPacketViewModel
            {
                Amount = amountFen,
                Count = count,
                Title = title
            };
            var response = await client.PostAsync("http://localhost:5000/api/values/red-packet/create", GetStringContent(input));
            if (response.IsSuccessStatusCode)
            {
                var res = await response.Content.ReadAsStringAsync();
                Console.WriteLine("红包创建成功:");
                Console.WriteLine(res);
            }
            else
            {
                Console.WriteLine("请求错误:" + response.StatusCode);
            }
        }

        /// <summary>
        /// 抢红包
        /// </summary>
        private static async Task GrabRedPacket()
        {
            using var client = new HttpClient();
            var response = await client.GetAsync("http://localhost:5000/api/values");
            if (response.IsSuccessStatusCode)
            {
                var res = await response.Content.ReadAsStringAsync();
                var list = JsonConvert.DeserializeObject<List<RedPacket>>(res);
                if (list != null && list.Count > 0)
                {
                    Console.WriteLine("以下是等待抢的红包:");
                    Console.WriteLine("-------------------------------------------------");
                    var numTitle = "序号".PadRight(8, ' ');
                    var amountTitle = "红包金额".PadRight(11, ' ');
                    var countTitle = "红包人数".PadRight(11, ' ');
                    var redTitle = "红包标题".PadRight(11, ' ');
                    Console.WriteLine($"{numTitle}{amountTitle}{countTitle}{redTitle}");
                    for (int i = 0; i < list.Count; i++)
                    {
                        var num = list[i].Id.ToString().PadRight(10, ' ');
                        var amount = (list[i].Amount / 100).ToString().PadRight(15, ' ');
                        var count = list[i].Count.ToString().PadRight(15, ' ');
                        Console.WriteLine($"{num}{amount}{count}{list[i].Title}");
                    }
                    Console.WriteLine("-------------------------------------------------");
                    Console.WriteLine("请输入要抢的红包序号:");
                    var s = Console.ReadLine();
                    if (long.TryParse(s, out long id))
                    {
                        Console.WriteLine("请输入要抢红包人数:");
                        var sCount = Console.ReadLine();
                        if (int.TryParse(sCount, out int count))
                        {
                            await GrabRedPacket(id, count);
                        }
                        else
                        {
                            Console.WriteLine("您输入的抢红包人数不正确,程序结束");
                        }
                    }
                    else
                    {
                        Console.WriteLine("您输入的序号不正确,程序结束");
                    }
                }
                else
                {
                    Console.WriteLine("没有红包可抢,请先去发给红包吧");
                }
            }
            else
            {
                Console.WriteLine("请求错误:" + response.StatusCode);
            }
        }

        static async Task GrabRedPacket(long redPacketId, int count)
        {
            for (int i = 0; i < count; i++)
            {
                await GrabRedPacket(redPacketId, Guid.NewGuid().ToString());
            }
            var totalAmount = Amounts.Sum() / (100 * 1.0);
            Console.WriteLine($"抢红包操作完成,一共抢了{totalAmount}元");
        }

        static async Task GrabRedPacket(long redPacketId, string user)
        {
            var input = new
            {
                CreateUser = user,
                RedPacketId = redPacketId
            };
            using var client = new HttpClient();
            var response = await client.PostAsync("http://localhost:5000/api/values/red-packet-record/create", GetStringContent(input));
            if (response.IsSuccessStatusCode)
            {
                var res = await response.Content.ReadAsStringAsync();
                var redPacketRecord = JsonConvert.DeserializeObject<RedPacketRecord>(res);
                Console.WriteLine(redPacketRecord.Message);
                Amounts.Add(redPacketRecord.Amount);
            }
            else
            {
                Console.WriteLine("请求错误:" + response.StatusCode);
            }
            await Task.Delay(100);
        }

        static StringContent GetStringContent(object content)
        {
            var setting = new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            };
            var body = JsonConvert.SerializeObject(content, setting);
            var httpContext = new StringContent(body, Encoding.UTF8);
            httpContext.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return httpContext;
        }
    }
}
