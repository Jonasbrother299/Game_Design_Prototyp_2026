using System;

public readonly struct HexCoord : IEquatable<HexCoord>
{
	public readonly int Q;
	public readonly int R;

	public HexCoord(int q, int r)
	{
		Q = q;
		R = r;
	}

	public bool Equals(HexCoord other)
	{
		return Q == other.Q && R == other.R;
	}

	public override bool Equals(object obj)
	{
		return obj is HexCoord other && Equals(other);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Q, R);
	}

	public override string ToString()
	{
		return $"Q:{Q}, R:{R}";
	}
}
