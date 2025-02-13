using Newtonsoft.Json;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Aggregation;
using StackExchange.Redis;

/*
 we can use redis to store session data as well as cache data
 */

// Connect to the server (localhost)
ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost,allowAdmin=true");
var endpoints = redis.GetEndPoints();
var server = redis.GetServer(endpoints.FirstOrDefault());

server.FlushDatabase(); // to delete all keys in the database (flushall command)

#region Contant

IDatabase db = redis.GetDatabase();
ISearchCommands ft = db.FT(); // is then used to create and manage search indexes
var json = db.JSON();

#endregion

#region Redis in C#

fillData(db);
GroupByCity(db);


void fillData(IDatabase db)
{
	var user1 = new
	{
		name = "Ali",
		email = "Ali@gmail.com",
		age = 25,
		city = "Cairo"
	};
	var user2 = new
	{
		name = "Asmaa",
		email = "asmaa@gmail.com",
		age = 23,
		city = "Alex"
	};
	var user3 = new
	{
		name = "Rahma",
		email = "rahma@gmail.com",
		age = 22,
		city = "Tanta"
	};
	var user4 = new
	{
		name = "Mohmed",
		email = "mohmed@gmail.com",
		age = 21,
		city = "Tanta"
	};

	var schema = new Schema()
		.AddTextField(new FieldName("$.name", "name"))
		.AddTagField(new FieldName("$.email", "email"))
		.AddTagField(new FieldName("$.city", "city"))
		.AddNumericField(new FieldName("$.age", "age"));


	// create index to every key start with user:
	ft.Create(
		"idx:users",
		new FTCreateParams().On(NRedisStack.Search.Literals.Enums.IndexDataType.JSON).Prefix("user:"),
		schema);

	json.Set("user:1", "$", user1);
	json.Set("user:2", "$", user2);
	json.Set("user:3", "$", user3);
	json.Set("user:4", "$", user4);
}

void SearchByAge(IDatabase db)
{
	Console.WriteLine("******************Search by age********************");

	var result = ft.Search("idx:users", new Query("@age:[25 30]")).Documents.Select(x => x["json"]);
	Console.WriteLine(string.Join("\n", result));
}

void SearchByName(IDatabase db)
{
	Console.WriteLine("******************Search by name********************");

	var result = ft.Search("idx:users", new Query("Rahma")).Documents.Select(x => x["json"]);
	Console.WriteLine(string.Join("\n", result));
}

// only return city
void SearchByNameAndReturnCity(IDatabase db)
{
	Console.WriteLine("******************Search by name and return city********************");

	var result = ft.Search("idx:users", new Query("Rahma").ReturnFields(new FieldName("$.city", "city"))).Documents.Select(x => x["city"]);
	Console.WriteLine(string.Join("\n", result));
}

void GroupByCity(IDatabase db)
{
	Console.WriteLine("******************Group by city********************");

	var request = new AggregationRequest("*").GroupBy("@city", Reducers.Count().As("count"));
	var result = ft.Aggregate("idx:users", request);

	for (var i = 0; i < result.TotalResults; i++)
	{
		var row = result.GetRow(i);
		Console.WriteLine(row["city"] + " = " + row["count"]);
	}
}

#endregion

var options = new ConfigurationOptions
{
	EndPoints = { "localhost:8001" },
	Password = "password",
};

/*
 to connect to cluster
options = new ConfigurationOptions
{
	EndPoints = { "localhost:8001", "localhost:8002", "localhost:8003" }
}
write host and port of master node only
 */


/*
interfaces:
1. IDatabase
2. ISubscriber
3. ITransaction
4. IServer
5. IConnectionMultiplexer

1.IconnectionMultiplexer
direct all client command to redies server
through two connections one for commands(interactive) and the other for subscriptions (pop/sub)
return three objects:
1.implemetation of IDatabase
2.implemetation of ISubscriber
3.implemetation of IServer

1.IDatabase
to send command to redis server
commands of ds

2.Iserver
to deal with instance of redis server or specific nodes
commands of server
IconnectionMultiplexer.GetServer("localhost:8001")

3.ISubscriber
to deal with pub/sub
commands of pub/sub
publish message or subscribe to channel
IconnectionMultiplexer.GetSubscriber()

4.ITransaction
to deal with transaction
commands of transaction
IDatabase.CreateTransaction()
call execute method
to get instance of ITransaction use IDatabase.GetTransaction()
 */


// Get a reference to the database (connect to database)
//IDatabase db = redis.GetDatabase();

//to make transaction
async Task TransactionSampleAsync(IDatabase db)
{
	var transaction = db.CreateTransaction();
	await transaction.HashSetAsync("Person:1", new HashEntry[]
	{
	new ("name", "Ali"),
	new ("age", 25),
	new ("city", "Cairo")
	});
	await transaction.SortedSetAddAsync("Leaders", new SortedSetEntry[]
	{
	new ("Mohmed" , 5),
	new ("Ali" , 3),
	new ("Rahma" , 5)
	});

	var success = transaction.Execute();
}

//XADDSample(db);

void SimpleStringSample(IDatabase db)
{
	db.StringSet("name", "Rahma Mohmed");
	Console.WriteLine(db.StringGet("name"));
	Console.ReadLine();
}

void IncrementSample(IDatabase db)
{
	db.StringSet("counter", 1);
	Console.WriteLine(db.StringGet("counter"));
	db.StringIncrement("counter");
	Console.WriteLine(db.StringGet("counter"));
	Console.ReadLine();
}

void IncrementBySample(IDatabase db)
{
	db.StringSet("counter", 1);
	Console.WriteLine(db.StringGet("counter"));
	db.StringIncrement("counter", 5);
	Console.WriteLine(db.StringGet("counter"));
	Console.ReadLine();
}

void GETSETSample(IDatabase db)
{
	Console.WriteLine(db.StringGetSet("name", "Ali"));
	Console.WriteLine(db.StringGetSet("name", "Asmaa"));
	Console.ReadLine();
}

void MSSample(IDatabase db)
{
	db.StringSet(new KeyValuePair<RedisKey, RedisValue>[]
	{
			new ("name", "Ali"),
			new ("age", 25),
			new ("city", "Cairo")
	});

	var values = db.StringGet(new RedisKey[] { "name", "age", "city" });
	foreach (var value in values)
	{
		Console.WriteLine(value);
	}
	Console.ReadLine();
}

void SimpleJsonSample(IDatabase db)
{
	IJsonCommands json = db.JSON();
	json.Set("animal", "$", "\"cat\"");
	Console.WriteLine("animal value: " + json.Get("animal"));
	Console.WriteLine("animal type: " + json.Type("animal").FirstOrDefault());
	Console.WriteLine("animal length: " + json.StrLen("animal").FirstOrDefault());
	Console.ReadLine();
}

void SimpleListSample(IDatabase db)
{
	db.ListRightPush("names", "Ali");
	db.ListRightPush("names", "Asmaa");
	db.ListRightPush("names", "Rahma");
	db.ListRightPush("names", "Mohmed");
	var names = db.ListRange("names", 0, -1);
	foreach (var name in names)
	{
		Console.WriteLine(name);
	}

	Console.WriteLine("Names[0] = " + db.ListGetByIndex("names", 0));
	Console.WriteLine("Names[1] = " + db.ListGetByIndex("names", 1));

	Console.WriteLine("LPOP = " + db.ListLeftPop("names"));
	Console.WriteLine("RPOP = " + db.ListRightPop("names"));

	Console.WriteLine("LLEN = " + db.ListLength("names"));

	Console.ReadLine();
}

void simpleListMoveSample(IDatabase db)
{
	db.ListLeftPush("list1", "a");
	db.ListLeftPush("list1", "b");
	db.ListLeftPush("list1", "c");

	db.ListLeftPush("list2", "x");
	db.ListLeftPush("list2", "y");
	db.ListLeftPush("list2", "z");

	db.ListMove("list1", "list2", ListSide.Right, ListSide.Left);

	Console.WriteLine("==========List1===========");
	var list1 = db.ListRange("list1", 0, -1);
	foreach (var item in list1)
	{
		Console.WriteLine(item);
	}

	Console.WriteLine("==========List2===========");
	var list2 = db.ListRange("list2", 0, -1);
	foreach (var item in list2)
	{
		Console.WriteLine(item);
	}
}

// flushall command to delete all keys in the database

// set unordered collection

void SimpleSetSample(IDatabase db)
{
	db.SetAdd("set", "Ali");
	db.SetAdd("set", "Asmaa");
	db.SetAdd("set", "Rahma");
	db.SetAdd("set", "Mohmed");
	var names = db.SetMembers("set");
	foreach (var name in names)
	{
		Console.WriteLine(name);
	}
	Console.WriteLine("SISMEMBER = " + db.SetContains("set", "Ali"));
	Console.WriteLine("SISMEMBER = " + db.SetContains("set", "Alii"));
	Console.WriteLine("SPOP = " + db.SetPop("set"));
	Console.WriteLine("SPOP = " + db.SetPop("set"));
	Console.WriteLine("SCARD = " + db.SetLength("set"));
	Console.ReadLine();
}

void HSetSample(IDatabase db)
{
	db.HashSet("person", new HashEntry[]
	{
			new ("name", "Ali"),
			new ("age", 25),
			new ("city", "Cairo")
	});
	var values = db.HashGet("person", new RedisValue[] { "name", "age", "city" });
	foreach (var value in values)
	{
		Console.WriteLine(value);
	}
	Console.ReadLine();
}

//GetAll
void HGetAllSample(IDatabase db)
{
	var values = db.HashGetAll("person");
	foreach (var value in values)
	{
		Console.WriteLine(value.Name + " = " + value.Value);
	}
	Console.ReadLine();
}

void HIncrBySample(IDatabase db)
{
	Console.WriteLine("HINCRBY = " + db.HashIncrement("person", "age", 5));
	Console.ReadLine();
}

void ZADDSample(IDatabase db)
{
	db.SortedSetAdd("Leaders10", new SortedSetEntry[]
	{
		new SortedSetEntry("Mohmed" , 5),
		new SortedSetEntry("Ali" , 3),
		new SortedSetEntry("Rahma" , 5)
	});

	var members = db.SortedSetRangeByRank("Leaders10", 0, -1);
	foreach (var member in members)
	{
		Console.WriteLine(member);
	}
}

void ZREVRANGESAMPLE(IDatabase db)
{
	var members = db.SortedSetRangeByRank("Leaders10", 0, -1, order: Order.Descending);
	foreach (var member in members)
	{
		Console.WriteLine(member);
	}
}

void ZRANGEBYSCORESAMPLE(IDatabase db)
{
	var members = db.SortedSetRangeByScore("Leaders10", 2, 4);
	foreach (var member in members)
	{
		Console.WriteLine(member);
	}
}

void ZRANKSAMPLE(IDatabase db)
{
	var index = db.SortedSetRank("Leaders10", "Ali");
	Console.WriteLine(index);
}

void ZREVRANKSAMPLE(IDatabase db)
{
	var index = db.SortedSetRank("Leaders10", "Ali", order: Order.Descending);
	Console.WriteLine(index);
}

void XADDSample(IDatabase db)
{
	var entry1 = db.StreamAdd("stream", new NameValueEntry[]
	{
		new ("name", "Ali"),
		new ("age", 25),
	},
	maxLength: 2);

	var entry2 = db.StreamAdd("stream", new NameValueEntry[]
	{
		new ("name", "Asmaa"),
		new ("age", 23),
	},
	maxLength: 2);

	long startID = ((DateTimeOffset)DateTime.Now.AddHours(-1)).ToUnixTimeMilliseconds();
	long endID = ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds();

	var entries = db.StreamRange("stream", startID, endID);
	foreach (var entry in entries)
	{
		Console.WriteLine($"Entry ID = {entry.Id}, Entry value = " + JsonConvert.SerializeObject(entry.Values));
	}

	Console.WriteLine("===================================");
	Console.WriteLine($"we will delete entry ID = {entry1}");

	db.StreamDelete($"stream", new[] { entry1 });

	Console.WriteLine("Entities after delete");
	var entriesAfterDelete = db.StreamRange("stream", startID, endID);

	foreach (var entry in entriesAfterDelete)
	{
		Console.WriteLine($"Entry ID = {entry.Id}, Entry value = " + JsonConvert.SerializeObject(entry.Values));
	}

	Console.ReadLine();
}

void XRANGE_REV_SAMPLE(IDatabase db)
{
	var entry1 = db.StreamAdd("stream", new NameValueEntry[]
	{
		new ("name", "Ali"),
		new ("age", 25),
	});
	var entry2 = db.StreamAdd("stream", new NameValueEntry[]
	{
		new ("name", "Asmaa"),
		new ("age", 23),
	});
	var entry3 = db.StreamAdd("stream", new NameValueEntry[]
	{
		new ("name", "Rahma"),
		new ("age", 22),
	});
	var entry4 = db.StreamAdd("stream", new NameValueEntry[]
	{
		new ("name", "Mohmed"),
		new ("age", 21),
	});

	var entries = db.StreamRange("stream", "-", "+", count: 2, messageOrder: Order.Descending);
	foreach (var entry in entries)
	{
		Console.WriteLine($"Entry ID = {entry.Id}, Entry value = " + JsonConvert.SerializeObject(entry.Values));
	}
}

void XREAD_SAMPLE(IDatabase db)
{
	var entry1 = db.StreamAdd("stream", new NameValueEntry[]
	{
		new ("name", "Ali"),
		new ("age", 25),
	});
	var entry2 = db.StreamAdd("stream", new NameValueEntry[]
	{
		new ("name", "Asmaa"),
		new ("age", 23),
	});
	var entry3 = db.StreamAdd("stream", new NameValueEntry[]
	{
		new ("name", "Rahma"),
		new ("age", 22),
	});
	var entry4 = db.StreamAdd("stream", new NameValueEntry[]
	{
		new ("name", "Mohmed"),
		new ("age", 21),
	});

	long startId = ((DateTimeOffset)DateTime.Now.AddHours(-1)).ToUnixTimeMilliseconds();

	var entries = db.StreamRead("stream", startId, count: 1);

	foreach (var entry in entries)
	{
		Console.WriteLine($"Entry ID = {entry.Id}, Entry value = " + JsonConvert.SerializeObject(entry.Values));
	}
	Console.ReadLine();
}

void XGROUP_SAMPLE(IDatabase db)
{
	var entry1 = db.StreamAdd("stream", new NameValueEntry[]
	{
		new ("name", "Ali"),
		new ("age", 25),
	});
	var entry2 = db.StreamAdd("stream", new NameValueEntry[]
	{
		new ("name", "Asmaa"),
		new ("age", 23),
	});
	var entry3 = db.StreamAdd("stream", new NameValueEntry[]
	{
		new ("name", "Rahma"),
		new ("age", 22),
	});
	var entry4 = db.StreamAdd("stream", new NameValueEntry[]
	{
		new ("name", "Mohmed"),
		new ("age", 21),
	});

	var groupCreated = db.StreamCreateConsumerGroup("stream", "group1", createStream: true);
	Console.WriteLine("Group created: " + groupCreated);

	var entries = db.StreamReadGroup("stream", "group1", "consumer1", count: 1);
	foreach (var entry in entries)
	{
		Console.WriteLine($"Entry ID = {entry.Id}, Entry value = " + JsonConvert.SerializeObject(entry.Values));
	}
	Console.ReadLine();
}
