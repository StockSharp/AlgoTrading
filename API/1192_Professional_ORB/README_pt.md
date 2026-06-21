# Estratégia Profissional ORB
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementa uma estratégia de Rompimento do Intervalo de Abertura. A máxima e a mínima entre 09:15 e uma duração configurável formam o intervalo. Após o intervalo ser concluído e amplo o suficiente, rompimentos acima ou abaixo acionam entradas compradas ou vendidas. As posições usam um stop-loss baseado em ATR, um alvo de lucro fixo em pontos e são fechadas no final da sessão. O número de operações por dia é limitado.
