using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApi.Data;
using Wei.RedisHelper;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        readonly RedisClient _client;
        readonly ILogger<ValuesController> _logger;
        public ValuesController(RedisClient client, ILogger<ValuesController> logger)
        {
            _client = client;
            _logger = logger;
        }

        [HttpGet]
        public async Task<List<RedPacket>> Get()
        {

            var list = await _client.HashValuesAsync<RedPacket>(nameof(RedPacket));
            list = list.Where(x => x.RemainingCount > 0).OrderByDescending(x => x.Id).ToList();
            return list;
        }

        // POST api/values
        [HttpPost("red-packet/create")]
        public async Task<IActionResult> CreateRedPacket([FromBody] RedPacketViewModel viewModel)
        {
            if (viewModel.Amount < viewModel.Count)
            {
                return BadRequest("红包个数必须小于红包金额(分)");
            }
            var redPacket = new RedPacket
            {
                Id = await _client.StringIncrementAsync($"{nameof(RedPacket)}_ID"),
                Amount = viewModel.Amount,
                Count = viewModel.Count,
                RemainingAmount = viewModel.Amount,
                RemainingCount = viewModel.Count,
                Title = viewModel.Title
            };
            await _client.HashSetAsync(nameof(RedPacket), redPacket.Id.ToString(), redPacket);
            return Ok(redPacket);
        }

        [HttpPost("red-packet-record/create")]
        public async Task<IActionResult> CreateRedPacketRecord([FromBody] RedPacketRecordViewModel viewModel)
        {

            var redPacketRecord = new RedPacketRecord
            {
                CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CreateUser = viewModel.CreateUser,
                RedPacketId = viewModel.RedPacketId
            };
            var redPacketId = viewModel.RedPacketId.ToString();
            await GrabRedPacket(redPacketId, redPacketRecord);


            return Ok(redPacketRecord);
        }

        private async Task GrabRedPacket(string redPacketId, RedPacketRecord redPacketRecord)
        {
            bool? tryAgain;
            do
            {
                var token = Environment.MachineName;
                if (_client.Database.LockTake(redPacketId, token, TimeSpan.FromSeconds(10)))
                {
                    try
                    {
                        var redPacket = await _client.HashGetAsync<RedPacket>(nameof(RedPacket), redPacketId);
                        redPacket.RemainingCount--;
                        if (redPacket.RemainingCount >= 0)
                        {
                            redPacketRecord.Id = await _client.StringIncrementAsync($"{nameof(RedPacketRecord)}_ID");
                            if (redPacket.RemainingCount == 0)
                            {
                                //最后一人
                                redPacketRecord.Amount = redPacket.RemainingAmount;
                                redPacket.RemainingAmount = 0;
                            }
                            else
                            {
                                //抢到红包最多为剩余人均的2倍
                                var maxAmount = (redPacket.RemainingAmount / (redPacket.RemainingCount + 1) * 2 - 1);
                                redPacketRecord.Amount = new Random().Next(1, maxAmount);
                                redPacket.RemainingAmount -= redPacketRecord.Amount;
                            }
                            await _client.HashSetAsync(nameof(RedPacket), redPacketId, redPacket);
                            await _client.HashSetAsync(nameof(RedPacketRecord), redPacketRecord.Id.ToString(), redPacketRecord);
                            tryAgain = false;
                            _logger.LogError($"成功抢红包{redPacketRecord.Amount / (100 * 1.0)}元:{redPacketRecord.CreateUser}");
                        }
                        else
                        {
                            //抢完了
                            tryAgain = null;
                            _logger.LogError("抢完了");
                        }
                    }
                    finally
                    {
                        _client.Database.LockRelease(redPacketId, token);
                    }
                }
                else
                {
                    //锁住了,正在抢购中,等待所释放后再次抢
                    tryAgain = true;
                    await Task.Delay(50);
                }
            } while (tryAgain.HasValue && tryAgain.Value);

        }
    }



    public class RedPacketViewModel
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public int Amount { get; set; }
        [Required]
        public int Count { get; set; }
    }

    public class RedPacketRecordViewModel
    {
        [Required]
        public long RedPacketId { get; set; }

        [Required]
        public string CreateUser { get; set; }
    }
}
