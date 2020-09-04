using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApi.Data
{
    /// <summary>
    /// 红包类
    /// </summary>
    public class RedPacket
    {
        /// <summary>
        /// 主键
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 金额（单位分，1元=100分）
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        /// 红包个数
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 红包剩余金额（单位分，1元=100分）
        /// </summary>
        public int RemainingAmount { get; set; }

        /// <summary>
        /// 红包剩余数量
        /// </summary>
        public int RemainingCount { get; set; }


        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    /// <summary>
    /// 红包记录
    /// </summary>
    public class RedPacketRecord
    {
        public long Id { get; set; }
        /// <summary>
        /// 红包Id
        /// </summary>
        public long RedPacketId { get; set; }

        /// <summary>
        ///  抢到的红包金额（单位分）
        /// </summary>

        public int Amount { get; set; }

        /// <summary>
        /// 抢到的红包金额（单位元）
        /// </summary>
        public string Message
        {
            get
            {
                if (Amount > 0)
                {
                    return $"恭喜[{CreateUser}]:您抢到红包【{Amount / (100 * 1.0)}】元";
                }
                else
                {
                    return $"很遗憾[{CreateUser}]:您手速太慢了,没有抢到……";
                }
            }
        }

        /// <summary>
        /// 抢红包的人
        /// </summary>
        public string CreateUser { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public string CreateTime { get; set; }
    }




}
