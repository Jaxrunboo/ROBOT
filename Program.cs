using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Robot
{
    class Program
    {
        public static void Main(string[] args)
        {

            /*
             * 目的： 减少代码操作，或者不进行代码操作，所有人可用
             * 配置项： auth验证  
             * { 
             *  chennelid:'',
             *  content:'',
             *  turn:0,//ms
             *  proxy:''
             * }
             * 暂时不处理代理问题，一机一用
             * 考虑队列处理
             */

            //数据录入
            string setFilePath = System.Environment.CurrentDirectory + "\\AppSetting.json";
            var setStr = File.ReadAllText(setFilePath);
            var set = JsonConvert.DeserializeObject<SetDto>(setStr);
            var msgSet = set.Set;

            //记录信息
            Task.Run(async () =>
            {
                int turns = 0;
                while (true)
                {
                    var interval = new TimeSpan(0, 0, (int)msgSet.Seconds);
                    string url = $"https://discord.com/api/v9/channels/{msgSet.ChannelId}/messages";
                    string resStatus = default;

                    try
                    {
                        throw new Exception();
                        int length = msgSet.Content.Length;
                        Random rand = new Random();
                        var randIndex = rand.Next(0, length - 1);

                        var cont = msgSet.Content[randIndex];
                        var res = await Post(url, cont, msgSet.Auth, msgSet.Nonce);
                        resStatus = "success";
                       
                    }
                    catch (Exception ex)
                    {
                        //日志
                        Log(ex.Message);
                        resStatus = "fail";
                    }
                    turns++;
                    Console.WriteLine($"{msgSet.ProjectName}-{msgSet.ChannelId}-{turns}-{resStatus}");
                    Thread.Sleep(interval);
                }
            }).Wait();

        }

        public static async Task<string> Post(string url, string content, string auth, string nonce)
        {
            //header
            HttpClient client = new HttpClient();
            
            client.DefaultRequestHeaders.Add("authorization", auth);
            //client.DefaultRequestHeaders.Add("Content-type", "application/json");
            //cont
            var json = new
            {
                content = content,
                nonce = string.IsNullOrWhiteSpace(nonce) ? DateTime.Now.ToString() : nonce,
                tts = false
            };
            HttpContent cont = new StringContent(JsonConvert.SerializeObject(json));
            cont.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var res = await client.PostAsync(url, cont);
            string resStr = default;
            if (res.IsSuccessStatusCode)
            {
                resStr = await res.Content.ReadAsStringAsync();
            }


            return resStr;
        }


        public class SetDto
        {
            public DisMessage Set { get; set; }
        }


        public class DisMessage
        {
            public string ProjectName { get; set; }
            /// <summary>
            /// 频道id
            /// </summary>
            public string ChannelId { get; set; }
            /// <summary>
            /// 发送文本
            /// </summary>
            public string[] Content { get; set; }
            /// <summary>
            /// 等待时间
            /// </summary>
            public int Seconds { get; set; }
            /// <summary>
            /// 代理ip
            /// </summary>
            public string ProxyIP { get; set; }
            /// <summary>
            /// 账号唯一验证
            /// </summary>
            public string Auth { get; set; }
            /// <summary>
            /// 随机数
            /// </summary>
            public string Nonce { get; set; }
        }

        //读写锁，当资源处于写入模式时，其他线程写入需要等待本次写入结束之后才能继续写入
        static ReaderWriterLockSlim logWriteLock = new ReaderWriterLockSlim();

        public static void Log(string msg)
        {
            try
            {
                logWriteLock.EnterWriteLock();

                string path = System.Environment.CurrentDirectory + "\\" + DateTime.Now.ToString("MM-dd")+".log";
                File.AppendAllText(path, msg);
            }
            catch (Exception)
            {

            }
            finally
            {
                logWriteLock.ExitWriteLock();
            }

        }

    }
}
