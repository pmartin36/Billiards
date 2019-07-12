using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class MinMax {
	public int Min { get; set; }
	public int Max { get; set; }

	public int Range { 
		get => Max - Min;
	}

	public MinMax(int min, int max) {
		Min = min;
		Max = max;
	}
}

