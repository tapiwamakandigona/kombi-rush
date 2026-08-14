using System;
using System.Globalization;
using System.Text;
using KombiRush.Sim;
using KombiRush.Tests;

public static class Dump
{
    public static void Main(string[] args)
    {
        uint seed = 20260814u;
        float target = args.Length > 0 ? float.Parse(args[0], CultureInfo.InvariantCulture) : 52f;
        var cfg = SimConfig.Default();
        var up = new UpgradeSet();
        up.SetLevel(UpgradeId.Engine, 1);
        up.SetLevel(UpgradeId.Seats, 1);
        var sim = new RoadSim(cfg, up, seed);
        float dt = 1f / 60f;
        for (int i = 0; i < (int)(target / dt) && !sim.IsOver; i++)
        {
            if (i % 4 == 0) Autopilot.Decide(sim);
            sim.Step(dt);
        }
        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append($"  \"laneCount\": {cfg.LaneCount}, \"laneWidth\": {F(cfg.LaneWidth)},\n");
        sb.Append($"  \"playerY\": {F(sim.PlayerY)}, \"laneF\": {F(sim.LaneF)}, \"targetLane\": {sim.TargetLane},\n");
        sb.Append($"  \"speed\": {F(sim.Speed)}, \"fuel\": {F(sim.Fuel)}, \"fuelCap\": {F(sim.FuelCapacity)},\n");
        sb.Append($"  \"hull\": {sim.Hull}, \"hullMax\": {sim.HullMax}, \"riders\": {sim.Riders}, \"seats\": {sim.SeatCapacity},\n");
        sb.Append($"  \"cash\": {sim.CashCents}, \"banked\": {sim.BankedFareCents}, \"combo\": {F(sim.Combo)},\n");
        sb.Append($"  \"elapsed\": {F(sim.ElapsedSeconds)},\n  \"entities\": [\n");
        bool first = true;
        foreach (Entity e in sim.Entities)
        {
            if (!e.Alive) continue;
            if (!first) sb.Append(",\n");
            first = false;
            sb.Append($"    {{\"kind\": \"{e.Kind}\", \"lane\": {e.Lane}, \"span\": {e.Span}, \"y\": {F(e.Y)}}}");
        }
        sb.Append("\n  ]\n}\n");
        Console.Write(sb.ToString());
    }
    static string F(float v) => v.ToString("0.####", CultureInfo.InvariantCulture);
}
