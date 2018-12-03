using System;
using System.Collections.Generic;
using MessagePack;
using Newtonsoft.Json;

namespace MarginTrading.IntegrationTests.Models
{
    [MessagePackObject]
    public sealed class OrderBook : IKeyedObject, ICloneable
    {
        public OrderBook(string source, string assetPairId, DateTime timestamp, 
            IReadOnlyCollection<VolumePrice> asks, IReadOnlyCollection<VolumePrice> bids)
        {
            Source = source;
            AssetPairId = assetPairId;
            Timestamp = timestamp;
            Asks = asks;
            Bids = bids;
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
        [IgnoreMember]
        public string Key => $"{Source}_{AssetPairId}";

        public object Clone()
        {
            return new OrderBook(Source, AssetPairId, Timestamp, Asks, Bids);
        }
    }

    [MessagePackObject]
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
