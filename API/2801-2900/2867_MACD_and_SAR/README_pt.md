# Estratégia MACD e SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o consultor especialista original do MetaTrader "MACD and SAR". Avalia a relação entre as linhas principal e de sinal do MACD juntamente com o nível do SAR Parabólico a cada vela concluída. Interruptores configuráveis permitem inverter cada comparação para que o mesmo modelo possa ser usado tanto para configurações contra-tendência quanto a favor da tendência. Múltiplas entradas são permitidas desde que o número máximo configurado de posições empilhadas não seja excedido.

Quando um sinal de compra aparece, a exposição vendida existente é fechada e um novo lote comprado é aberto (se o limite não for atingido). Da mesma forma, um sinal de venda fecha os comprados primeiro e depois adiciona um lote vendido. Não há ordens adicionais de stop-loss ou take-profit; as operações são fechadas apenas quando o sinal oposto é gerado.

## Lógica da estratégia

1. Aguardar uma vela concluída do período configurado.
2. Ler os valores do MACD (principal, sinal, histograma) e o nível do SAR Parabólico calculado sobre preços de fechamento.
3. Avaliar as seguintes comparações, cada uma das quais pode ser invertida pelo seu parâmetro booleano correspondente:
   - Linha principal do MACD vs. linha de sinal.
   - Linha de sinal do MACD vs. nível zero.
   - SAR Parabólico vs. preço de fechamento.
4. Se as três comparações para o lado comprado forem satisfeitas e a estratégia ainda tiver espaço para empilhar novas posições, comprar o tamanho de lote especificado (incluindo o volume necessário para fechar vendidos).
5. Se as três comparações para o lado vendido forem satisfeitas e o limite de empilhamento permitir, vender o tamanho de lote especificado (incluindo o volume necessário para fechar comprados).

## Parâmetros

- `TradeVolume` — volume por operação individual (padrão `0.1`).
- `MaxPositions` — número máximo de posições empilhadas em uma direção (padrão `10`).
- `MacdFastPeriod` — período da EMA rápida do MACD (padrão `12`).
- `MacdSlowPeriod` — período da EMA lenta do MACD (padrão `26`).
- `MacdSignalPeriod` — período de suavização do sinal do MACD (padrão `9`).
- `SarStep` — passo de aceleração do SAR Parabólico (padrão `0.02`).
- `SarMaximum` — aceleração máxima do SAR Parabólico (padrão `0.2`).
- `BuyMacdGreaterSignal` — se `true`, requer MACD principal > sinal para comprados; caso contrário, espera o oposto (padrão `true`).
- `BuySignalPositive` — se `true`, requer sinal MACD > 0 para comprados; caso contrário, espera sinal < 0 (padrão `false`).
- `BuySarAbovePrice` — se `true`, requer SAR acima do preço para comprados; caso contrário, espera preço acima do SAR (padrão `false`).
- `SellMacdGreaterSignal` — se `true`, requer MACD principal > sinal para vendidos; caso contrário, espera MACD principal < sinal (padrão `false`).
- `SellSignalPositive` — se `true`, requer sinal MACD > 0 para vendidos; caso contrário, espera sinal < 0 (padrão `true`).
- `SellSarAbovePrice` — se `true`, requer SAR acima do preço para vendidos; caso contrário, espera preço acima do SAR (padrão `true`).
- `CandleType` — tipo/período de vela usado para processamento de dados (padrão `15` minutos).

## Notas adicionais

- A estratégia depende exclusivamente de cruzamentos de indicadores; não há stops de proteção ou metas de lucro.
- O empilhamento de posições é implementado comparando o volume absoluto da posição com `MaxPositions * TradeVolume` com uma pequena tolerância para arredondamento.
- Todas as operações são executadas com ordens a mercado. Certifique-se de que a configuração de volume do portfólio corresponda aos instrumentos que pretende negociar.
- Adicione regras opcionais de proteção de portfólio se precisar de limites de drawdown ou stops de seguimento; nenhum está incluído por padrão.
