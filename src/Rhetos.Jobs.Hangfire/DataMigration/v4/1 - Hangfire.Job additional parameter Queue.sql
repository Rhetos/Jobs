/*DATAMIGRATION 758D252A-524F-4A4F-B691-B3962B0677DA*/ -- Change the script's code only if it needs to be executed again.

-- Adds the 'string Queue' parameter to the existing Hangfire jobs.
-- This allows for the existing jobs that were created with an older version of Rhetos.Jobs.Hangfire
-- to be executed by a newer version.
-- Additionally, it tries to reconstruct the original queue name that was specified when the job was enqueued.

IF OBJECT_ID('HangFire.Job') IS NOT NULL
EXEC sp_executesql
	N'
	UPDATE HangFire.Job
	SET
		InvocationData = REPLACE(InvocationData,
			''Rhetos.Jobs.Hangfire"]}'',
			''Rhetos.Jobs.Hangfire","System.String"]}''),
		Arguments = LEFT(Arguments, LEN(Arguments)-1) + '',"\"''
			+ ISNULL(SUBSTRING(s.Data, QueueNameStart, QueueNameEnd - QueueNameStart + 1), ''default'') -- QueueName
			+ ''\""]''
	FROM
		HangFire.Job j
		OUTER APPLY
		(
			SELECT TOP 1
				*,
				QueueNameStart = CHARINDEX(''"Queue":"'', s.Data) + 9,
				QueueNameEnd = LEN(s.Data) - 2
			FROM
				HangFire.State s
			WHERE
				s.JobId = j.Id
				AND s.Name = ''Enqueued''
			ORDER BY
				s.Id
		) s
	WHERE
		j.InvocationData LIKE ''%Rhetos.Jobs.Hangfire"]}%''
		AND j.StateName <> ''Succeeded'';
	';
