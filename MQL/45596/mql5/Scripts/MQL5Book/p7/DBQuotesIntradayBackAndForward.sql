-- this SQL statement expects 3 input parameters (back time, and forward time twice)
-- NB: if this text is processed by StringFormat-ing, '%' symbols should be doubled
-- in MQL5 code to preserve them for SQL processing
SELECT * FROM
(
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
         (LAG(open,-1) OVER (ORDER BY time) - open) * (open - LAG(open) OVER (ORDER BY time)) AS product,
         (LAG(open,-1) OVER (ORDER BY time) - open) * SIGN(open - LAG(open) OVER (ORDER BY time)) AS estimate
      FROM MqlRatesDB
      WHERE (time >= ?1 AND time < ?2)
   )
   GROUP BY intraday, day
) backtest
JOIN
(
   SELECT
      SUM(estimate) AS forward_profit,
      SUM(CASE WHEN estimate >= 0 THEN estimate ELSE 0 END) / SUM(CASE WHEN estimate < 0 THEN -estimate ELSE 0 END) AS forward_PF,
      intraday, day
   FROM
   (
      SELECT
         time,
         TIME(time, 'unixepoch') AS intraday,
         STRFTIME('%w', time, 'unixepoch') AS day,
         (LAG(open,-1) OVER (ORDER BY time) - open) * SIGN(open - LAG(open) OVER (ORDER BY time)) AS estimate
      FROM MqlRatesDB
      WHERE (time >= ?2)
   )
   GROUP BY intraday, day
) forward
USING(intraday, day)
ORDER BY objective DESC