# Estratégia de Impressões de Dark Pool
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
As Impressões de Dark Pool rastreiam grandes transações fora de bolsa que frequentemente precedem movimentos acentuados assim que a atividade é revelada.
Volume incomum na fita pode sinalizar posicionamento institucional que ainda não impactou o mercado regular.

Os testes indicam um retorno anual médio de aproximadamente 46%. Funciona melhor no mercado de ações.

A estratégia entra na mesma direção das grandes compras ou vendas do dark pool, esperando continuidade quando o restante do mercado reagir.

Um pequeno stop percentual mantém o risco contido e as posições são encerradas se o impulso esperado não se materializar.

### Regras executadas (C# e Python)

O padrão usa candles de 1 minuto e SMA20. Após 20 amostras de volume, o volume atual deve ser **estritamente maior** que 1,5 vez a média das 19 amostras anteriores. Apenas sem posição: compra se `Close > Open` e `Close > SMA20`; venda se `Close < Open` e `Close < SMA20`. Há espera de 500 candles concluídos entre novas entradas. Long é encerrado quando `Close < SMA20`, short quando `Close > SMA20`; **nem o volume nem a espera bloqueiam as saídas**. Posições executadas recebem stop nativo de 2%, sem take-profit. A rentabilidade citada acima não foi verificada independentemente no replay BTC/TON.


## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - \`CandleType\` = 1 minute
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

