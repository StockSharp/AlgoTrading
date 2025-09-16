using System;
using System.Collections.Generic;
using System.IO;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that writes best bid and ask prices to a binary file in batches.
/// </summary>
public class Demo_FileWriteArrayStrategy : Strategy
{
	private readonly StrategyParam<string> _fileName;
	private readonly StrategyParam<string> _directoryName;
	private readonly StrategyParam<int> _bufferSize;

	private PriceEntry[] _buffer;
	private int _index;
	private string _path;

	/// <summary>
	/// File name to write.
	/// </summary>
	public string FileName { get => _fileName.Value; set => _fileName.Value = value; }

	/// <summary>
	/// Directory where the file is stored.
	/// </summary>
	public string DirectoryName { get => _directoryName.Value; set => _directoryName.Value = value; }

	/// <summary>
	/// Number of records to accumulate before writing to file.
	/// </summary>
	public int BufferSize { get => _bufferSize.Value; set => _bufferSize.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="Demo_FileWriteArrayStrategy"/>.
	/// </summary>
	public Demo_FileWriteArrayStrategy()
	{
		_fileName = Param(nameof(FileName), "data.bin")
			.SetDisplay("File Name", "Name of output file", "General");

		_directoryName = Param(nameof(DirectoryName), "SomeFolder")
			.SetDisplay("Directory Name", "Folder to store file", "General");

		_bufferSize = Param(nameof(BufferSize), 20)
			.SetDisplay("Buffer Size", "Records before flush", "General")
			.SetGreaterThanZero();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_buffer = new PriceEntry[BufferSize];
		_index = 0;
		_path = Path.Combine(DirectoryName, FileName);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (!level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj) ||
			!level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj))
			return;

		var entry = new PriceEntry
		{
			Time = level1.ServerTime,
			Bid = (double)(decimal)bidObj,
			Ask = (double)(decimal)askObj
		};

		_buffer[_index++] = entry;

		if (_index == _buffer.Length)
		{
			WriteData(_index);
			_index = 0;
		}
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		if (_index > 0)
			WriteData(_index);

		_buffer = null;

		base.OnStopped();
	}

	private void WriteData(int count)
	{
		try
		{
			var directory = Path.GetDirectoryName(_path);
			if (!string.IsNullOrEmpty(directory))
				Directory.CreateDirectory(directory);

			using var stream = File.Open(_path, FileMode.Append, FileAccess.Write, FileShare.Read);
			using var writer = new BinaryWriter(stream);

			for (var i = 0; i < count; i++)
			{
				var item = _buffer[i];
				writer.Write(item.Time.UtcTicks);
				writer.Write(item.Bid);
				writer.Write(item.Ask);
			}
		}
		catch (Exception ex)
		{
			LogError(ex);
		}
	}

	private struct PriceEntry
	{
		public DateTimeOffset Time;
		public double Bid;
		public double Ask;
	}
}
