# Estratégia FineTuning MA Candle Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Porte em C# do assessor especialista MetaTrader 5 **Exp_FineTuningMACandle_Duplex**.
- Replica o indicador de vela FineTuningMA em dois fluxos independentes para que a lógica comprada e vendida possa ser ajustada separadamente.
- Projetada para a API de estratégia de alto nível do StockSharp: subscrições, indicadores, gerenciamento de risco e desenho de gráficos são todos gerenciados automaticamente pelo framework.

## Modelo de vela FineTuningMA
- O indicador original constrói uma vela sintética aplicando três exponentes ponderados (`Rank1`–`Rank3`) e coeficientes de deslocamento correspondentes às últimas `Length` barras.
- Os valores ponderados resultantes de abertura e fechamento são comparados para gerar um código de cor: `2` para altista, `1` para neutro, `0` para baixista.
- Quando o corpo real da vela é menor que o `Gap` configurável, a abertura sintética é igualada ao fechamento sintético anterior. Isso reproduz a lógica de "corpo plano" da versão MQL5.
- O indicador neste porte emite apenas o fluxo de cor (valores decimais 0/1/2) porque as regras de trading dependem exclusivamente das transições de cor.

## Lógica de trading
1. Subscreve dois feeds de velas (`LongCandleType` e `ShortCandleType`). Podem apontar para o mesmo período de tempo ou diferentes.
2. Para cada feed, uma instância dedicada do indicador FineTuningMA é criada com seus próprios parâmetros de ponderação e deslocamento de sinal (`SignalBar`).
3. Os eventos de vela completada são processados com as seguintes regras:
   - **Saída comprada** – se a cor anterior for igual a `0`, a posição comprada existente é fechada.
   - **Entrada comprada** – se a cor anterior for igual a `2` e a cor atual mudou de `2`, uma ordem de compra é enviada (após cobrir qualquer posição vendida).
   - **Saída vendida** – se a cor anterior for igual a `2`, a posição vendida existente é coberta.
   - **Entrada vendida** – se a cor anterior for igual a `0` e a cor atual mudou de `0`, uma ordem de venda é enviada (após cobrir qualquer posição comprada).
4. O volume da ordem é controlado por `OrderVolume`. Quando uma reversão é necessária, a estratégia adiciona automaticamente a posição atual absoluta para que a posição se inverta em uma única ordem a mercado.
5. Barreiras de proteção opcionais (`TakeProfitPoints`, `StopLossPoints`) são traduzidas em pontos de preço e aplicadas através de `StartProtection`.

## Parâmetros
### Fluxo comprado
- `LongCandleType` – tipo de dados de vela (período) para o fluxo do indicador comprado.
- `LongLength` – número de barras usadas no cálculo ponderado.
- `LongRank1`, `LongRank2`, `LongRank3` – coeficientes exponentes que moldam a curva de peso ao longo da janela de lookback.
- `LongShift1`, `LongShift2`, `LongShift3` – modificadores adicionais (0…1) que inclinam os pesos para o início ou o fim da janela.
- `LongGap` – tamanho máximo do corpo real da vela que mantém o preço sintético de abertura igual ao fechamento sintético anterior.
- `LongSignalBar` – quantas velas completadas ignorar antes de ler o sinal (`0` avalia a última vela fechada, `1` usa a anterior, etc.).
- `EnableLongEntries` – ativa as entradas compradas.
- `EnableLongExits` – ativa as saídas compradas automáticas.

### Fluxo vendido
- `ShortCandleType` – tipo de dados de vela para o fluxo do indicador vendido.
- `ShortLength`, `ShortRank1`, `ShortRank2`, `ShortRank3`, `ShortShift1`, `ShortShift2`, `ShortShift3`, `ShortGap`, `ShortSignalBar` – idênticos às suas contrapartes do lado comprado, mas aplicados ao fluxo vendido.
- `EnableShortEntries` – ativa as entradas vendidas.
- `EnableShortExits` – ativa as saídas vendidas automáticas.

### Trading
- `OrderVolume` – quantidade base para novas posições. Reversões adicionam automaticamente a posição atual absoluta a este valor.
- `TakeProfitPoints` – distância opcional de take-profit expressa em pontos de preço (0 a desabilita).
- `StopLossPoints` – distância opcional de stop-loss expressa em pontos de preço (0 a desabilita).

## Notas
- O assessor especialista original incluía modos de gerenciamento de capital baseados em saldo ou margem. O porte expõe um parâmetro `OrderVolume` fixo mais simples. Ajuste-o para corresponder ao dimensionamento de posição desejado.
- `StartProtection` é invocado apenas quando o instrumento expõe um passo de preço válido (`Security.Step > 0`).
- Nenhuma versão Python é fornecida intencionalmente.
- Áreas de gráfico são criadas automaticamente: se os feeds de velas compradas e vendidas diferirem, dois painéis separados são exibidos; caso contrário, apenas um gráfico é mostrado.
- A estratégia depende de velas completadas; não reage a atualizações intrabarra.
