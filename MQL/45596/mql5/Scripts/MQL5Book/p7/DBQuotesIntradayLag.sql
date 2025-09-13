-- this SQL statement expects 3 input parameters (start time, stop time, limit)
-- NB: if this text is processed by StringFormat-ing, '%' symbols should be doubled
-- in MQL5 code to preserve them for SQL processing
      SELECT
         DATETIME(time, 'unixepoch') as datetime,
         open,
         time,
         TIME(time, 'unixepoch') AS intraday,
         STRFTIME('%w', time, 'unixepoch') AS day,
         (LAG(open,-1) OVER (ORDER BY time) - open) AS delta,
         SIGN(open - LAG(open) OVER (ORDER BY time)) AS direction,
         (LAG(open,-1) OVER (ORDER BY time) - open) * (open - LAG(open) OVER (ORDER BY time)) AS product,
         (LAG(open,-1) OVER (ORDER BY time) - open) * SIGN(open - LAG(open) OVER (ORDER BY time)) AS estimate
      FROM MqlRatesDB
      WHERE (time >= ?1 AND time < ?2)
      ORDER BY time LIMIT ?3;