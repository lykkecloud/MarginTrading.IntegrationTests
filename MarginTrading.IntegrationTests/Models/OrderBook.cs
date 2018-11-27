using System;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;

namespace MarginTrading.IntegrationTests.Models
{
    [MessagePackObject(false)]
    public sealed class OrderBook : IKeyedObject, ICloneable
    {
        public OrderBook(string source, string assetPairId, IReadOnlyCollection<VolumePrice> asks, IReadOnlyCollection<VolumePrice> bids, DateTime timestamp)
        {
            Source = source;
            AssetPairId = assetPairId;
            Asks = asks;
            Bids = bids;
            Timestamp = timestamp;
        }

        [JsonProperty("source")]
        [Key(0)]
        public string Source { get; }

        [JsonProperty("asset")]
        [Key(1)]
        public string AssetPairId { get; }

        [JsonProperty("timestamp")]
        [Key(2)]
        public DateTime Timestamp { get; }

        [JsonProperty("asks")]
        [Key(3)]
        public IReadOnlyCollection<VolumePrice> Asks { get; }

        [JsonProperty("bids")]
        [Key(4)]
        public IReadOnlyCollection<VolumePrice> Bids { get; }

        [JsonIgnore]
        public string Key => $"{Source}_{AssetPairId}";

        public object Clone()
        {
            return new OrderBook(Source, AssetPairId, Asks, Bids, Timestamp);
        }
    }

    public sealed class VolumePrice
    {
        public VolumePrice(decimal price, decimal volume)
        {
            Price =  price;
            Volume = Math.Abs(volume);
        }

        [JsonProperty("volume")]
        [Key(0)]
        public decimal Volume { get; }

        [JsonProperty("price")]
        [Key(1)]
        public decimal Price { get; }

    }
}
