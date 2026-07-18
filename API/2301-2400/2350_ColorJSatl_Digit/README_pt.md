# Estratégia Color JSatl Digit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia converte o especialista MQL5 "Exp_ColorJSatl_Digit" para o StockSharp. Ela digitaliza a inclinação da Média Móvel Jurik (JMA) para classificar cada barra como alta ou baixa. Uma mudança do estado 0 para 1 marca uma tendência de alta emergente, enquanto uma mudança de 1 para 0 sinaliza uma tendência de baixa.

O algoritmo subscreve velas de um período escolhido e vincula um indicador JMA. Quando o JMA gira para cima, a estratégia abre uma posição comprada e fecha qualquer posição vendida. Quando o JMA gira para baixo, abre uma posição vendida e fecha qualquer posição comprada. O parâmetro opcional `DirectMode` inverte os sinais para operar contra a tendência.

As posições são protegidas por níveis de stop loss e take profit baseados em percentagem. Todos os parâmetros são definidos através de `StrategyParam` e podem ser otimizados.

## Detalhes

- **Critérios de entrada**
  - **Comprado**: JMA gira para cima (`prev > prevPrev` && `current >= prev`) e `DirectMode` é verdadeiro. No modo inverso, um giro para baixo abre a posição comprada.
  - **Vendido**: JMA gira para baixo (`prev < prevPrev` && `current <= prev`) e `DirectMode` é verdadeiro. No modo inverso, um giro para cima abre a posição vendida.
- **Critérios de saída**: O sinal oposto aciona uma ordem de mercado imediata na outra direção. Ordens de proteção também podem fechar posições.
- **Stops**: Stop loss e take profit percentual via `StartProtection`.
- **Valores padrão**
  - `JMA Length` = 30
  - `Candle Type` = velas de 4 horas
  - `Stop Loss %` = 1
  - `Take Profit %` = 2
  - `Direct Mode` = true
- **Filtros**
  - Categoria: Seguidor de tendência
  - Direção: Ambos (reversível)
  - Indicadores: Jurik Moving Average
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Moderado
