using System;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Redis
{
	struct Goods
	{
		public int Id;
		public string Title;
		public int Price;
		public Trader[] TradersRef;
	}

	struct Trader
	{
		public int Id;
		public string Name;
		public Goods[] GoodsRef;
	}

	class Program
	{
		private static IDatabase db;
		
		static void Main(string[] args)
		{
			var redis = ConnectionMultiplexer.Connect("localhost");
			db = redis.GetDatabase();
			SortedSetExample();

			Console.ReadKey();
		}

		private static void SimpleStoringExample()
		{
			db.StringSet("mykey", "хуй");
			var value = db.StringGet("mykey");
			Console.WriteLine(value);

			//Чистим хранилище

			db.KeyDelete("mykey");
		}

		private static void KeyWithLifetimeExample()
		{
			db.StringSet("expire-key", "value");
			db.KeyExpire("expire-key", TimeSpan.FromSeconds(15));
			Console.WriteLine("Ключ \"expire-key\" был создан, время жизни ключа — 15 секунд.");
			Task.Run(() =>
			{
				Thread.Sleep(TimeSpan.FromSeconds(7.5));
				Console.WriteLine("Оставшееся время существования ключа \"expire-key\": " + db.KeyTimeToLive("expireKey") + ".");
				Thread.Sleep(TimeSpan.FromSeconds(7.5));
				if (!db.KeyExists("expire-key"))
					Console.WriteLine("Время жизни ключа \"expire-key\" истекло!");
			});
		}

		private static void PersistentKeyExample()
		{
			db.StringSet("persistent-key", "value", TimeSpan.FromSeconds(5));
			Console.WriteLine("Ключ \"persistent-key\" был создан, время жизни ключа — 5 секунд.");
			Task.Run(() =>
			{
				Thread.Sleep(TimeSpan.FromSeconds(3));
				db.KeyPersist("persistent-key");
				Console.WriteLine("Кто-то пришел и сохранил жизнь этому парню — \"persistent-key\"!");
				Thread.Sleep(TimeSpan.FromSeconds(2));
				if (db.KeyExists("persistent-key"))
					Console.WriteLine("Ну вот, я же говорил что он(\"persistent-key\") не умрёт!");
			});

			//Чистим хранилище

			db.KeyDelete("persistent-key");
		}

		private static void DeleteKeyExample()
		{
			db.StringSet("delete-key", "value");
			Console.WriteLine("Ключ \"delete-key\" был создан.");
			if (db.KeyDelete("delete-key"))
				Console.WriteLine("Ключ \"delete-key\" был успешно удален!");
		}

		private static void ListOperationsExapmle()
		{
			// Добавление в список элемента (ключ создается автоматически)
			db.ListLeftPush("my-list", "middle");
			Console.WriteLine("Был создан ключ для списка \"my-list\" и вставлено значение: " + db.ListRange("my-list", 0 -1)[0]);
			db.ListRightPush("my-list", "jopa");
			db.ListLeftPush("my-list", "hoho");
			Console.WriteLine("В список было вставленно 2 элемента, один в хвост, другой в голову.");
			Console.WriteLine("Текущее состояние списка:");
			foreach (var value in db.ListRange("my-list"))
			{
				Console.WriteLine(value);
			}

			Console.WriteLine("Извлекаем два элемента с хвоста:");
			Console.WriteLine(db.ListRightPop("my-list"));
			Console.WriteLine(db.ListRightPop("my-list"));
			Console.WriteLine("Текущее состояние списка:");
			foreach (var value in db.ListRange("my-list"))
			{
				Console.WriteLine(value);
			}

			db.ListRightPush("my-list", "jopa");
			db.ListLeftPush("my-list", "hoho");
			Console.WriteLine("В список было вставленно 2 элемента, один в хвост, другой в голову.");
			Console.WriteLine("Текущее состояние списка:");
			foreach (var value in db.ListRange("my-list"))
			{
				Console.WriteLine(value);
			}

			Console.WriteLine("Ограничиваем список двумя элементами");
			db.ListTrim("my-list", 0, 1);
			Console.WriteLine("Текущее состояние списка:");
			foreach (var value in db.ListRange("my-list"))
			{
				Console.WriteLine(value);
			}
			Console.WriteLine("Длинна списка: " + db.ListLength("my-list"));

			// Чистим хранилище
			db.KeyDelete("my-list");
		}

		private static void HashExapmle()
		{
			var goods = new Goods
			{
				Id = 1,
				Title = "Чистый спирт",
				Price = 5000
			};

			Console.WriteLine("Записываем информацию о товаре:");
			Console.WriteLine("Наименование: " + goods.Title);
			Console.WriteLine("Цена: " + goods.Price);
			var hash = new []
			{
				new HashEntry("title", goods.Title),
				new HashEntry("price", goods.Price.ToString()), 
			};

			db.HashSet("goods:" + goods.Id, hash);

			// Специально выделил в отдельную переменную
			var requestingId = 1;
			var redisGoods = new Goods
			{
				Id = requestingId,
				Title = db.HashGet("goods:" + requestingId, "title"),
				Price = (int)db.HashGet("goods:" + requestingId, "price")
			};

			Console.WriteLine("Информация полученная о товаре:");
			Console.WriteLine("Наименование: " + redisGoods.Title);
			Console.WriteLine("Цена: " + redisGoods.Price);

			// Не забываем почистить хранилище
			db.KeyDelete("goods:" + goods.Id);
		}

		private static void SetExample()
		{
			// Список товаров
			var goods = new []
			{
				new Goods
				{
					Id = 1,
					Title = "Морковь мытая, кг.",
					Price = 40
				},
				new Goods
				{
					Id = 2,
					Title = "Чипсы картофельные Lays, большая",
					Price = 113
				},
				new Goods
				{
					Id = 3,
					Title = "Шоколадный батончик Snickers, 1 шт",
					Price = 29
				}
			};

			// Список торговцев
			var traders = new []
			{
				new Trader
				{
					Id = 1,
					Name = "Василий",
					GoodsRef = new []
					{
						goods[0]
					}
				},
				new Trader
				{
					Id = 2,
					Name = "Геннадий",
					GoodsRef = goods
				}
			};

			goods[0].TradersRef = new[] { traders[0], traders[1] };
			goods[1].TradersRef = new[] { traders[1] };
			goods[2].TradersRef = new[] { traders[1] };

			// Заполняем хэши
			foreach (var goodsRef in goods)
			{
				db.HashSet("goods:" + goodsRef.Id, new[]
				{
					new HashEntry("title", goodsRef.Title),
					new HashEntry("price", goodsRef.Price)
				});
			}

			foreach (var trader in traders)
			{
				db.HashSet("trader:" + trader.Id, new[]
				{
					new HashEntry("name", trader.Name)
				});
			}

			// Устанавливаем связь (вот для чего чаще всего используются Set)
			foreach (var trader in traders)
			{
				
				foreach (var goodsRef in trader.GoodsRef)
				{
					db.SetAdd("trader:" + trader.Id + ":goods", goodsRef.Id);
				}
			}

			foreach (var goodsRef in goods)
			{
				foreach (var trader in goodsRef.TradersRef)
				{
					db.SetAdd("goods:" + goodsRef.Id + ":traders", trader.Id);
				}
			}

			// Теперь ситуация, когда мне понадобилось получить товары которыми торгует
			// торговец c идентификатором 1

			var traiderId = 1;
			var goodsId = db.SetMembers("trader:" + traiderId + ":goods");
			Console.WriteLine("Торговец " + db.HashGet("trader:" + traiderId, "name") + " продает следующие товары:");
			foreach (var id in goodsId)
			{
				Console.WriteLine(db.HashGet("goods:" + id, "title") + " — " + db.HashGet("goods:" + id, "price") + " р.");
			}

			Console.Write("\n\n\n");

			// Теперь  ситуация, когда хочу узнать кто торгует только морковкой
			var goodsCarrotId = 1;
			var traidersId = db.SetMembers("goods:" + goodsCarrotId + ":traders");
			Console.WriteLine("Торговцы продающие товар — \"" + db.HashGet("goods:" + goodsCarrotId, "title") + "\":");
			foreach (var id in traidersId)
			{
				Console.WriteLine(db.HashGet("trader:" + id, "name"));
			}


			// Очищаем хранилище
			foreach (var goodsRef in goods)
			{
				db.KeyDelete("goods:" + goodsRef.Id);
			}

			foreach (var trader in traders)
			{
				db.KeyDelete("trader:" + trader.Id);
				// Валим связи
				db.KeyDelete("trader:" + trader.Id + ":goods");
			}
		}

		private static void SortedSetExample()
		{
			/*
			 * Здесь в примере приводится список торговцев,
			 * который составляет рейтинг продаж (по сумме)
			 */
			var traders = new[]
			{
				new Trader
				{
					Id = 1,
					Name = "Ашот",
				},
				new Trader
				{
					Id = 2,
					Name = "Рамзес"
				},
				new Trader
				{
					Id = 3,
					Name = "Альберт"
				},
				new Trader
				{
					Id = 4,
					Name = "Ванес"
				}
			};

			// То на какую сумму были совершены продажи
			var scores = new[]
			{
				115324.59,
				95942.44,
				78524.78,
				452378.15
			};

			for (var i = 0; i < 4 && traders.Length == 4 && scores.Length == 4; i++)
			{
				db.SortedSetAdd("traders-top", traders[i].Name, scores[i]);
			}
			
			// Хотим знать первые три места
			var top = db.SortedSetRangeByRankWithScores("traders-top", 0, 2, Order.Descending);
			Console.WriteLine("Топ 3 продавцов по продажам:");
			for (var i = 0; i < top.Length; i++)
			{
				Console.WriteLine($"{i + 1}-ое место по продажам занимает: {top[i].Element} ({top[i].Score} р.)");
			}

			Console.Write("\n\n\n");

			// Хотим узнать продавцов которые совершили продажи в диапазоне от 50 т. р. до 100 т. р.
			var between = db.SortedSetRangeByScore("traders-top", 50000, 100000);
			Console.WriteLine("Продавцы сделавшие продажи от 50 до 100 тыс. р.");
			foreach (var trader in between)
			{
				Console.WriteLine(trader);
			}

			// Чистим ключ
			db.KeyDelete("traders-top");
		}
	}
}
