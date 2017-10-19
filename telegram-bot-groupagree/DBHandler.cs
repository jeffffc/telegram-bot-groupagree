﻿using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using WJClubBotFrame;
using WJClubBotFrame.Types;

namespace telegrambotgroupagree {
	public class DBHandler {
		public DBHandler(string apikey, string host="localhost") {
			pollQueue = new List<QueueObject>();
			pointerQueue = new List<Pointer>();
			MySqlConnectionStringBuilder conn_string = new MySqlConnectionStringBuilder();
			this.apikey = apikey;
			string[] botsplit = apikey.Split(':');
			conn_string.Server = host;
			conn_string.UserID = botsplit[0];
			conn_string.Password = botsplit[1];
			conn_string.Database = botsplit[0];
			conn_string.Port = 3306;
			conn_string.CharacterSet = "utf8mb4";
			connection = new MySqlConnection(conn_string.ToString());
			
			try {
				Console.WriteLine("Trying to connect to: ..." + conn_string);
				Console.WriteLine("Connecting to MySQL...");
				connection.Open();
                connection.Close();
			} catch (Exception ex) {
				Console.WriteLine(ex.ToString());
				Console.WriteLine ("DB Error");
			}
			
		}

		private List<QueueObject> pollQueue;

		public List<QueueObject> PollQueue { get { return this.pollQueue; } }

		private List<Pointer> pointerQueue;

		public List<Pointer> PointerQueue { get { return this.pointerQueue; } }

		private MySqlConnection connection;

		private string apikey;

		public override string ToString() {
			return JsonConvert.SerializeObject(this, new JsonSerializerSettings {
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			});
		}

		public void AddToQueue(Poll poll, bool change = true) {
			pollQueue.RemoveAll(x => (x.Poll.ChatId == poll.ChatId && x.Poll.PollId == poll.PollId));
			pollQueue.Add(new QueueObject {
				Poll = poll,
				Change = change,
			});
		}

		public List<Poll> GetPolls(long? chatId = null, int? pollId = null, string searchFor = null, bool reverse = false, int limit = 50) {
			connection.Open();
			List<Poll> allPolls = new List<Poll>();
			MySqlCommand command = connection.CreateCommand();
			command.CommandText = "SELECT * FROM polls WHERE";
			if (chatId != null) {
				command.CommandText += " chatid = '" + chatId + "'";
				if (pollId != null) {
					command.CommandText += " AND pollid = '" + pollId + "'";
				} else if (searchFor != null && searchFor != "") {
					searchFor = "%" + searchFor + "%";
					command.CommandText += " AND pollText LIKE ?searchFor";
					command.Parameters.AddWithValue("?searchFor", searchFor);
				}
				command.CommandText += " AND archived = 1";
			} else {
				command.CommandText += " archived = 0";
			}
			if (reverse)
				command.CommandText += " ORDER BY pollid DESC LIMIT " + limit;
			command.CommandText += ";";
			try {
				MySqlDataReader reader;
				reader = command.ExecuteReader();
				while (reader.Read()) {
					switch ((EPolls)Enum.Parse(typeof(EPolls), reader["pollType"].ToString())) {
						case EPolls.vote:
							allPolls.Add(new PVote(int.Parse(reader["chatid"].ToString()), int.Parse(reader["pollid"].ToString()), reader["pollText"].ToString(), reader["pollDescription"].ToString(), (EAnony)Enum.Parse(typeof(EAnony), reader["anony"].ToString()), (reader["closed"].ToString() == "1" || false), (PercentageBars.Bars)Enum.Parse(typeof(PercentageBars.Bars), reader["percentageBar"].ToString()), (reader["appendable"].ToString() == "1" || false), (reader["sorted"].ToString() == "1" || false), (reader["archived"].ToString() == "1" || false), JsonConvert.DeserializeObject<Dictionary<string, List<User>>>(reader["pollVotes"].ToString()), JsonConvert.DeserializeObject<List<MessageID>>(reader["messageIds"].ToString()), this, (Strings.langs)Enum.Parse(typeof(Strings.langs), reader["lang"].ToString())));
							break;
						case EPolls.doodle:
							allPolls.Add(new Doodle(int.Parse(reader["chatid"].ToString()), int.Parse(reader["pollid"].ToString()), reader["pollText"].ToString(), reader["pollDescription"].ToString(), (EAnony)Enum.Parse(typeof(EAnony), reader["anony"].ToString()), (reader["closed"].ToString() == "1" || false), (PercentageBars.Bars)Enum.Parse(typeof(PercentageBars.Bars), reader["percentageBar"].ToString()), (reader["appendable"].ToString() == "1" || false), (reader["sorted"].ToString() == "1" || false), (reader["archived"].ToString() == "1" || false), JsonConvert.DeserializeObject<Dictionary<string, List<User>>>(reader["pollVotes"].ToString()), JsonConvert.DeserializeObject<List<MessageID>>(reader["messageIds"].ToString()), JsonConvert.DeserializeObject<List<User>>(reader["people"].ToString()), this, (Strings.langs)Enum.Parse(typeof(Strings.langs), reader["lang"].ToString())));
							break;
						case EPolls.board:
							allPolls.Add(new Board(int.Parse(reader["chatid"].ToString()), int.Parse(reader["pollid"].ToString()), reader["pollText"].ToString(), reader["pollDescription"].ToString(), (EAnony)Enum.Parse(typeof(EAnony), reader["anony"].ToString()), (reader["closed"].ToString() == "1" || false), (reader["archived"].ToString() == "1" || false), this, JsonConvert.DeserializeObject<Dictionary<int, BoardVote>>(reader["pollVotes"].ToString()), JsonConvert.DeserializeObject<List<MessageID>>(reader["messageIds"].ToString()), (Strings.langs)Enum.Parse(typeof(Strings.langs), reader["lang"].ToString())));
							break;
						case EPolls.limitedDoodle:
							allPolls.Add(new LimitedDoodle(int.Parse(reader["chatid"].ToString()), int.Parse(reader["pollid"].ToString()), reader["pollText"].ToString(), reader["pollDescription"].ToString(), (EAnony)Enum.Parse(typeof(EAnony), reader["anony"].ToString()), (reader["closed"].ToString() == "1" || false), (PercentageBars.Bars)Enum.Parse(typeof(PercentageBars.Bars), reader["percentageBar"].ToString()), (reader["appendable"].ToString() == "1" || false), (reader["sorted"].ToString() == "1" || false), (reader["archived"].ToString() == "1" || false), JsonConvert.DeserializeObject<Dictionary<string, List<User>>>(reader["pollVotes"].ToString()), JsonConvert.DeserializeObject<List<MessageID>>(reader["messageIds"].ToString()), JsonConvert.DeserializeObject<List<User>>(reader["people"].ToString()), int.Parse(reader["maxVotes"].ToString()), this,  (Strings.langs)Enum.Parse(typeof(Strings.langs),reader["lang"].ToString())));
							break;
					}
				}
			} catch (Exception e) {
				Notifications.log(e.ToString());
			} finally {
				connection.Close();
			}
			return allPolls;
		}

		public void AddToQueue(Pointer pointer) {
			try {
				pointerQueue.RemoveAll(x => x.ChatId == pointer.ChatId);
			} catch (NullReferenceException e) {
				Notifications.log("##_DBHANDLER_ADD_TO_QUEUE_##\n" + e.ToString());
			}
			pointerQueue.Add(pointer);
		}

		public Pointer GetPointer(int chatID) {
			connection.Open();
			MySqlCommand command = connection.CreateCommand();
			command.CommandText = "SELECT * FROM pointer WHERE chatId = " + chatID;
			Pointer pointer = null;
			try {
				MySqlDataReader reader;
				reader = command.ExecuteReader();
				if (reader.Read())
					pointer = ParsePointer(reader);
				return pointer;
			} catch (Exception e) {
				Notifications.log(e.ToString());
				throw new Exception("ChatID " + chatID + " has screwed up the pointer...");
			} finally {
				connection.Close();
			}
		}

		public List<Pointer> GetAllPointers() {
			connection.Open();
			List<Pointer> allPointers = new List<Pointer>();
			MySqlCommand command = connection.CreateCommand();
			command.CommandText = "SELECT * FROM pointer";
			try {
				MySqlDataReader reader;
				reader = command.ExecuteReader();
				while (reader.Read()) {
					allPointers.Add(ParsePointer(reader));
				}
			} catch (Exception e) {
				Notifications.log(e.ToString());
			} finally {
				connection.Close();
			}
			return allPointers;
		}

		Pointer ParsePointer(MySqlDataReader reader) {
			int? boardChatId = null, boardPollId = null;
			int temp;
			boardChatId = int.TryParse(reader["boardChatId"].ToString(), out temp) ? (int?)temp : null;
			boardPollId = int.TryParse(reader["boardPollId"].ToString(), out temp) ? (int?)temp : null;
			return new Pointer(int.Parse(reader["chatId"].ToString()), (EPolls)Enum.Parse(typeof(EPolls), reader["pollType"].ToString()), (ENeedle)Enum.Parse(typeof(ENeedle), reader["needle"].ToString()), (EAnony)Enum.Parse(typeof(EAnony), reader["anony"].ToString()), boardChatId, boardPollId, int.Parse(reader["lastPollId"].ToString()), (Strings.langs)Enum.Parse(typeof(Strings.langs), reader["lang"].ToString()));
		}

		public void FlushToDB(Strings strings) {
			connection.Open();
			pollQueue.ForEach(x => x.Poll.GenerateCommand(connection, apikey, strings, x.Change).ExecuteNonQuery());
			pollQueue.Clear();
			try {
				pointerQueue.ForEach(x => x.GenerateCommand(connection).ExecuteNonQuery());
				pointerQueue.Clear();
			} catch (Exception e) {
				Notifications.log(e.ToString());
			}
			connection.Close();
		}
	}

	public class QueueObject {
		public Poll Poll;
		public bool Change;
	}
}