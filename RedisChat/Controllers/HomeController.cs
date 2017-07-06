using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace RedisChat.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Announce()
        {
            var pub = Program.Redis.GetSubscriber();
            await pub.PublishAsync(ChatSocket.ChatChannel, "Hello World");

            return Ok();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
