# Estratégia de Impressões de Dark Pool
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este exemplo aproxima atividades de grande porte pelo **volume dos candles**, não por registros reais de dark pools. Não distingue execuções em bolsa de negócios fora de bolsa. Negocia um sinal proxy de preço, SMA e pico de volume, não um feed de transações dark pool. Retornos históricos não são comprovados pelo código nem pelo replay BTC/TON.

### Regras executadas (C# e Python)

O padrão usa candles de 1 minuto e SMA20. Após 20 amostras de volume, o volume atual deve ser **estritamente maior** que 1,5 vez a média das 19 amostras anteriores. Apenas sem posição: compra se `Close > Open` e `Close > SMA20`; venda se `Close < Open` e `Close < SMA20`. Há espera de 500 candles concluídos entre novas entradas. Long é encerrado quando `Close < SMA20`, short quando `Close > SMA20`; **nem o volume nem a espera bloqueiam as saídas**. Posições executadas recebem stop nativo de 2%, sem take-profit. A rentabilidade citada acima não foi verificada independentemente no replay BTC/TON.


## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 1 minute
  - `StopLossPercent` = 2%
- **Filtros**:
  - Categoria: Volume
  - Direção: Ambos
  - Indicadores: Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

