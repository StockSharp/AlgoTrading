# Estratégia de Impressões de Dark Pool
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Negocia relatórios de grandes operações fora de bolsa na direção do agressor informado, usando a SMA20 do instrumento negociado como filtro de tendência.

`Security` define o instrumento negociado e `DarkPoolSecurity` um fluxo separado de relatórios do mesmo ativo subjacente. Os identificadores devem ser diferentes. O fluxo deve fornecer negócios reais fora de bolsa (`IsSystem = false`), preço, volume, horário e `OriginSide`. Fonte ausente ou idêntica impede a inicialização. Negócios em bolsa, relatórios sem lado, valores inválidos, duplicados e relatórios fora de ordem não geram entradas.

O volume deve ser estritamente maior que `1.5 × média dos 20 volumes anteriores`; o relatório atual fica fora da média. Compra diante de compra informada acima da SMA20 e vende diante de venda abaixo dela. Cor e volume das velas não substituem esses dados. Um sinal contrário reverte toda a posição executada com `Volume + abs(Position)`.

A SMA usa velas concluídas de 15 minutos. Fecha compras abaixo da SMA20 e vendas acima, independentemente do volume dos relatórios e da pausa entre entradas. O padrão é `CooldownBars = 0`; valores positivos atrasam novas entradas pelo número de velas concluídas. Ordens pendentes bloqueiam sinais sobrepostos.

`StopLoss = 2` configura proteção nativa local de 2% a partir do preço real de execução. A proteção acompanha bid/ask e envia saída a mercado; entre velas, exige fluxo de cotações. Não há take-profit.
