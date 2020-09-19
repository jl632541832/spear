﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spear.Core;
using Spear.Core.Config;
using Spear.Core.Dependency;
using Spear.Core.Helper;
using Spear.Core.Micro;
using Spear.Core.Micro.Services;
using Spear.Core.Serialize;
using Spear.Core.Session;
using Spear.Core.Extensions;
using Spear.Protocol.Tcp;
using Spear.ProxyGenerator;
using Spear.Tests.Client.Services;
using Spear.Tests.Client.Services.Impl;
using Spear.Tests.Contracts;
using Spear.Tests.Contracts.Dtos;
using System;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Spear.Core.Tests;

namespace Spear.Tests.Client
{
    public class Client
    {
        public static void Start(params string[] args)
        {
            var builder = new MicroBuilder();
            builder.AddLogging(builder =>
             {
                 builder.SetMinimumLevel(LogLevel.Information);
                 builder.AddConsole();
             });
            var services = builder
                .AddMicroClient(builder =>
                {
                    builder
                        .AddJsonCodec()
                        //.AddMessagePackCodec()
                        //.AddProtoBufCodec()
                        .AddSession()
                        //.AddHttpProtocol()
                        .AddTcpProtocol()
                        //.AddWebSocketProtocol()
                        //.AddGrpcProtocol()
                        //.AddNacos(opt =>
                        //{
                        //    opt.Host = "http://192.168.0.231:8848/";
                        //    opt.Tenant = "ef950bae-865b-409b-9c3b-bc113cf7bf37";
                        //})
                        //.AddConsul("http://192.168.0.231:8500")
                        .AddDefaultRouter(r =>
                        {
                            r.Regist("Spear.Tests.Contracts_v1", new ServiceAddress("127.0.0.1", 5003)
                            {
                                Service = "192.168.10.217",
                                Protocol = ServiceProtocol.Tcp,
                                Codec = ServiceCodec.Json,
                                Gzip = false
                            });
                        })
                        ;
                });
            services.AddSingleton<IService, ServieA>();
            services.AddSingleton<IService, ServieB>();
            var provider = services.BuildServiceProvider();

            CurrentIocManager.CreateLogger<Client>().LogInformation("test");

            var logger = provider.GetService<ILogger<Client>>();
            logger.LogInformation("请输入消息");
            while (true)
            {
                var message = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(message))
                    continue;
                //SingleTest(provider, message, logger);
                CodeTimerTest(provider, message, logger);
            }
        }

        private static void SingleTest(IServiceProvider provider, string message, ILogger logger)
        {
            var proxy = provider.GetService<IProxyFactory>();
            var client = proxy.Create<Account.AccountClient>();
            var result = client.Login(new LoginRequest
            {
                Account = message,
                Password = RandomHelper.RandomNums(6),
                Code = RandomHelper.RandomLetters(4)
            });
            logger.LogInformation(JsonHelper.ToJson(result));

            //var watch = Stopwatch.StartNew();
            //var proxy = provider.GetService<IProxyFactory>();
            //var service = proxy.Create<ITestContract>();
            //if (isNotice)
            //{
            //    service.Notice(message).GetAwaiter().GetResult();
            //}
            //else
            //{
            //    var msg = service.Get(message).Result;
            //    logger.Debug(msg);
            //}
            //watch.Stop();
            //logger.Info(watch.ElapsedMilliseconds);
        }

        private static void CodeTimerTest(IServiceProvider provider, string message, ILogger logger)
        {
            var msgArgs = message.Split(new[] { ",", ";", " " }, StringSplitOptions.RemoveEmptyEntries);
            int repeat = 1, thread = 1;
            var isNotice = true;
            if (msgArgs.Length > 1)
                repeat = msgArgs[1].CastTo(1);
            if (msgArgs.Length > 2)
                thread = msgArgs[2].CastTo(1);
            if (msgArgs.Length > 3)
                isNotice = msgArgs[3].CastTo(true);
            message = msgArgs[0];
            Task.Run(async () =>
            {
                var proxy = provider.GetService<IProxyFactory>();
                //var service = proxy.Create<Account.AccountClient>();
                var service = proxy.Create<ITestContract>();

                var result = await CodeTimer.Time("micro test", repeat, async () =>
                {
                    try
                    {
                        var name = RandomHelper.RandomLetters(5);
                        var m = $"hello {name}";
                        if (RandomHelper.Random().Next(10) > 1)
                        {
                            m += " loged";
                            var accessor = provider.GetService<IPrincipalAccessor>();
                            accessor.SetSession(new SessionDto
                            {
                                UserId = name,
                                UserName = name,
                                Role = name
                            });
                        }
                        //var header = new Metadata();
                        //var dto = await service.LoginAsync(new LoginRequest
                        //{
                        //    Account = message,
                        //    Password = RandomHelper.RandomNumAndLetters(6),
                        //    Code = RandomHelper.RandomLetters(4)
                        //});
                        //logger.LogInformation(JsonHelper.ToJson(dto));

                        if (isNotice)
                        {
                            await service.Notice(m);
                        }
                        else
                        {
                            //var msg = await service.Get(m);
                            var user = await service.User(new UserInputDto
                            { Id = RandomHelper.Random().Next(1000, 10000), Name = message });
                            logger.LogInformation(JsonHelper.ToJson(user));
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, ex.Message);
                        throw;
                    }
                }, thread);
                Console.WriteLine(result.ToString());
                //Counter.Show();
                //Counter.Clear();
            });
        }
    }
}
