# Estratégia ProMart MACD Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port do StockSharp do especialista MQL histórico **MartGreg_1 / ProMart**. Ela combina duas configurações MACD com um modelo de dimensionamento de posição martingale controlado. O MACD primário busca mínimos e máximos locais no momentum, enquanto o MACD secundário confirma a direção da inclinação recente. Após cada operação fechada, a estratégia segue o padrão do indicador novamente (quando a última operação foi lucrativa) ou imediatamente inverte a direção (após uma perda) enquanto potencialmente dobra o tamanho da próxima ordem.

## Lógica de trading

- **Sinais**
  - Construir dois indicadores MACD na série de velas selecionada:
    - `MACD1` (rápido=5, lento=20, sinal=3) atua como detector de padrões.
    - `MACD2` (rápido=10, lento=15, sinal=3) confirma a inclinação de curto prazo.
  - Avaliar sinais apenas em velas concluídas usando os três valores MACD1 anteriores e os dois valores MACD2 anteriores (refletindo a lógica MQL que olhava uma barra atrás).
  - **Configuração comprada**: MACD1 forma um vale local (`MACD1[t-1] > MACD1[t-2] < MACD1[t-3]`) e MACD2 está subindo (`MACD2[t-2] > MACD2[t-1]`).
  - **Configuração vendida**: MACD1 forma um pico local enquanto MACD2 está caindo.
  - Se a última operação fechada foi lucrativa, a estratégia aguarda o próximo setup válido. Após uma operação perdedora abre a direção oposta imediatamente, independentemente da forma atual do MACD, replicando a reversão martingale original.
- **Gestão de posições**
  - As operações são abertas com ordens de mercado e monitoradas em cada vela concluída.
  - Os níveis de stop-loss e take-profit são calculados em pontos de preço a partir do preço de entrada. Se o máximo/mínimo da vela atingir qualquer nível, a posição é fechada a mercado e o resultado da operação é registrado.
  - Nenhuma nova operação é aberta na mesma vela que fechou uma posição; a estratégia aguarda a próxima barra, assim como o especialista MQL que agia no primeiro tick de uma nova barra.
- **Dimensionamento martingale**
  - Um volume base é derivado do patrimônio do portfólio dividido por `BalanceDivider` e alinhado ao passo de volume do instrumento (recorrendo à propriedade `Volume` ou ao volume mínimo do instrumento quando necessário).
  - Após uma operação perdedora, a próxima posição pode dobrar o volume da ordem anterior, até `MaxDoublingCount` vezes consecutivas. A lucratividade reinicia o contador de doblamento.
  - O volume é sempre limitado pelo volume máximo do instrumento para evitar o superdimensionamento.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `BalanceDivider` | Divisor aplicado ao patrimônio do portfólio para calcular o volume base da ordem. | `1000` |
| `MaxDoublingCount` | Número máximo de dobramentos de volume consecutivos após perdas. | `1` |
| `StopLossPoints` | Distância do stop-loss medida em pontos de preço (`PriceStep * StopLossPoints`). | `500` |
| `TakeProfitPoints` | Distância do take-profit medida em pontos de preço. | `1500` |
| `Macd1Fast` / `Macd1Slow` / `Macd1Signal` | Períodos para o MACD primário que detecta vales/picos. | `5 / 20 / 3` |
| `Macd2Fast` / `Macd2Slow` / `Macd2Signal` | Períodos para o filtro de inclinação do MACD secundário. | `10 / 15 / 3` |
| `CandleType` | Tipo de dados da série de velas (padrão: período de 1 minuto). | `TimeSpan.FromMinutes(1).TimeFrame()` |

## Notas

- A implementação aproxima os preenchimentos de stop-loss e take-profit intrabar usando os máximos e mínimos das velas porque o exemplo do StockSharp opera em velas concluídas.
- O volume da posição recorre à propriedade `Volume` da estratégia ou ao volume mínimo do instrumento quando os dados do portfólio não estão disponíveis.
- Ainda não há versão Python; apenas a estratégia C# está incluída.
- Sempre validar a configuração em dados históricos antes de habilitar o trading real. O componente martingale aumenta significativamente o risco.
