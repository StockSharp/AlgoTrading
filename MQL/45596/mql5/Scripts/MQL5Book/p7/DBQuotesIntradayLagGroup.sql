-- this SQL statement expects 2 input parameters (start time, stop time)
-- NB: if this text is processed by StringFormat-ing, '%' symbols should be doubled
-- in MQL5 code to preserve them for SQL processing
   SELECT
      AVG(product) / STDDEV(product) AS objective,
      SUM(estimate) AS backtest_profit,
      SUM(CASE WHEN estimate >= 0 THEN estimate ELSE 0 END) / SUM(CASE WHEN estimate < 0 THEN -estimate ELSE 0 END) AS backtest_PF,
      intraday, day
   FROM
   (
      SELECT
         time,
         TIME(time, 'unixepoch') AS intraday,
         STRFTIME('%w', time, 'unixepoch') AS day,
         (LAG(open,-1) OVER (ORDER BY time) - open) AS delta,
         SIGN(open - LAG(open) OVER (ORDER BY time)) AS direction,
         (LAG(open,-1) OVER (ORDER BY time) - open) * (open - LAG(open) OVER (ORDER BY time)) AS product,
         (LAG(open,-1) OVER (ORDER BY time) - open) * SIGN(open - LAG(open) OVER (ORDER BY time)) AS estimate
      FROM MqlRatesDB
      WHERE (time >= ?1 AND time < ?2)
   )
   GROUP BY intraday, day
   ORDER BY objective DESC