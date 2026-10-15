# Estratégia Momo Trades
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Conversão do expert advisor original do MetaTrader "Momo_trades" que opera rompimentos de momentum filtrados por uma média móvel e estrutura do MACD.

## Lógica da estratégia
- Trabalha com velas concluídas do período configurado e processa apenas uma posição líquida por vez.
- Usa uma média móvel simples com um deslocamento de barra configurável para medir o quanto o preço fechou afastado da média. Operações compradas exigem que o fechamento deslocado esteja acima da SMA em mais do que o limiar de deslocamento de preço; vendidas exigem o contrário.
- Avalia um padrão de momentum MACD em cascata que espelha as regras MQL: vários valores passados da linha principal MACD devem aumentar através de zero para comprados ou diminuir através de zero para vendidos. Isso evita operações enquanto o momentum está se enfraquecendo.
- Abre uma ordem a mercado com o volume da estratégia assim que tanto o filtro de distância SMA quanto o padrão MACD se alinham para a mesma direção.

## Gestão de risco
- Stop-loss, take-profit, trailing stop, passo de trailing, break-even e inputs de deslocamento de preço são definidos em pips e automaticamente convertidos para unidades de preço usando o passo do instrumento.
- Quando valores de take-profit e trailing são fornecidos, o stop só é arrastado após o preço avançar pela distância de trailing mais o passo de trailing, reproduzindo o comportamento MQL.
- Quando nenhum take-profit está configurado mas uma distância de break-even está definida, o stop é movido para o preço de entrada assim que o gatilho de break-even é atingido.
- Todos os níveis de stop e take são recalculados a cada vela concluída e fechados por ordens a mercado quando cruzados pelos extremos da vela.

## Gestão de sessão
- A bandeira `CloseEndDay` corresponde ao expert advisor original e fecha qualquer posição ativa às 23h00 hora da plataforma (21h00 nas sextas-feiras). Após o corte, a estratégia ignora novas entradas até o dia seguinte.

## Parâmetros
- **SMA Period / MA Bar Shift** – comprimento da média móvel e o índice de barra usado para obter valores de SMA e preço.
- **MACD Fast / Slow / Signal / Bar Shift** – configuração do MACD e o deslocamento aplicado aos valores armazenados para verificações de padrão.
- **Stop Loss / Take Profit / Trailing Stop / Trailing Step / Breakeven / Price Shift** – distâncias em pips que controlam a saída, o trailing e os filtros de SMA.
- **Close End Of Day** – fecha posições após o fim de sessão configurado.
- **Candle Type** – período usado para velas e cálculos de indicadores.
