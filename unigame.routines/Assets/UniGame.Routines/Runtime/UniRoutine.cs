﻿namespace UniGreenModules.UniRoutine.Runtime {
	using System.Collections;
	using System.Collections.Generic;
	using Interfaces;
	using UniCore.Runtime.Interfaces;
	using UniCore.Runtime.ObjectPool.Runtime;
	using UniCore.Runtime.ObjectPool.Runtime.Extensions;
	using UniCore.Runtime.ProfilerTools;

	public class UniRoutine : IUniRoutine, IResetable
	{
		private int idCounter = 1;
		private Dictionary<int,UniRoutineTask> activeRoutines = new Dictionary<int, UniRoutineTask>();
		
		private List<UniRoutineTask> routines = new List<UniRoutineTask>(200);
		private List<UniRoutineTask> bufferRoutines = new List<UniRoutineTask>(200);
		
		public IUniRoutineTask AddRoutine(IEnumerator enumerator,bool moveNextImmediately = true) {

			if (enumerator == null) return null;
			
			var routine = ClassPool.Spawn<UniRoutineTask>();

#if UNITY_EDITOR
			if (routine.IsCompleted == false) {
				GameLog.LogError("ROUTINE: routine task is not completed");
			}
#endif
			var id = idCounter++;
			//get routine from pool
			routine.Initialize(id,enumerator, moveNextImmediately);

			routines.Add(routine);
			activeRoutines[id] = routine;
			
			return routine;
		}

		public bool CancelRoutine(int id)
		{
			if(activeRoutines.TryGetValue(id, out var routineTask))
			{
				routineTask.Dispose();
				return true;
			}
			return false;
		}
		
		/// <summary>
		/// update all registered routine tasks
		/// </summary>
		public void Update() {

			bufferRoutines.Clear();
			
			for (var i = 0; i < routines.Count; i++) {
				//execute routine
				var routine = routines[i];
				var moveNext = false;
				
				moveNext = routine.MoveNext();

				//copy to buffer routine
				if (moveNext) {
					bufferRoutines.Add(routine);
					continue;
				}

				CancelRoutine(routine.IdValue);
				routine.Despawn();
			}

			var swapBuffer = bufferRoutines;
			bufferRoutines = routines;
			routines = swapBuffer;
			
		}

		public void Reset()
		{
			for (var i = 0; i < routines.Count; i++) {
				var routine = routines[i];
				routine.Complete();
				routine.Despawn();
			}
			
			activeRoutines.Clear();
			routines.Clear();
			bufferRoutines.Clear();
		}
	}
}
