# Estratégia de comércio de automóveis NNFX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **NNFX Auto Trade Strategy** replica o fluxo de trabalho de gerenciamento e dimensionamento de risco do painel NNFX MetaTrader 4 original dentro de StockSharp. Em vez de uma interface gráfica, a estratégia expõe comandos manuais através de parâmetros. Os traders podem solicitar entradas longas ou curtas, nivelar instantaneamente a exposição ou aplicar o ponto de equilíbrio e a lógica de rastreamento que refletem o consultor especialista.

Características principais:

- Dimensionamento de volatilidade orientado por ATR com uma substituição opcional para distâncias manuais de parada e lucro.
- As entradas de posição são divididas em duas partes: uma com uma meta projetada e um corredor que fica aberto para gerenciamento discricionário.
- Os comandos de ponto de equilíbrio e trailing operam sob demanda, atualizando os níveis de stop armazenados sem disparar automaticamente em cada barra.
- Capital adicional pode ser incluído ao calcular o risco monetário, correspondendo ao comportamento do script MQL.

## Lógica de negociação
1. **ATR coleção** – A estratégia assina o tipo de vela configurado e processa um indicador Average True Range. Quando `UsePreviousDailyAtr` está ativado, ele copia o valor ATR do dia anterior durante as primeiras 12 horas do novo dia de negociação, imitando o script original.
2. **Dimensionamento baseado em risco** – Em um comando manual `Buy` ou `Sell`, o mecanismo calcula o risco monetário por unidade usando a distância de parada protetora e converte a porcentagem de risco desejada em um volume executável.
3. **Divisão de posição** – O volume de entrada é dividido em duas metades. A primeira metade é liquidada automaticamente quando o alvo projetado é tocado, enquanto a segunda metade permanece até que o trader emita novos comandos.
4. **Tratamento de stop** – Os stops iniciais são armazenados internamente e avaliados em cada vela finalizada. Os comandos manuais podem empurrar o stop para o ponto de equilíbrio ou avançá-lo de acordo com a fórmula móvel NNFX.
5. **Controles de saída** – `CloseAll` nivela imediatamente o livro, enquanto violações de stop ou metas parciais acionam saídas de mercado que respeitam os volumes calculados.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `RiskPercent` | `2.0` | Porcentagem do patrimônio da conta (mais `AdditionalCapital`) arriscado por negociação. |
| `AdditionalCapital` | `0` | Capital extra adicionado à base patrimonial no dimensionamento de posições. |
| `UseAdvancedTargets` | `false` | Muda as distâncias de risco de múltiplos de ATR para valores de pip manuais. |
| `AdvancedStopPips` | `0` | Pare a distância em pips quando o modo avançado estiver ativo. |
| `AdvancedTakeProfitPips` | `0` | Distância alvo em pips para a saída parcial quando o modo avançado estiver ativo. |
| `UsePreviousDailyAtr` | `true` | Copia o diário anterior ATR durante as primeiras 12 horas de um novo dia. |
| `AtrPeriod` | `14` | ATR comprimento de lookback. |
| `AtrStopMultiplier` | `1.5` | Multiplicador aplicado a ATR ao calcular a distância de parada. |
| `AtrTakeProfitMultiplier` | `1.0` | Multiplicador aplicado a ATR ao calcular a distância de realização do lucro. |
| `CandleType` | `1 Minute` | Tipo de vela usado para ATR e monitoramento de preços. |
| `BuyCommand` | `false` | Sinalizador manual – definido como `true` para solicitar uma entrada longa. Reinicia automaticamente. |
| `SellCommand` | `false` | Sinalizador manual – definido como `true` para solicitar uma entrada curta. Reinicia automaticamente. |
| `BreakevenCommand` | `false` | Sinalizador manual – mova o stop de proteção para o preço de entrada. Reinicia automaticamente. |
| `TrailingCommand` | `false` | Sinalizador manual – aplique a fórmula final NNFX uma vez. Reinicia automaticamente. |
| `CloseAllCommand` | `false` | Sinalização manual – feche todas as posições abertas instantaneamente. Reinicia automaticamente. |

## Notas de uso
- A estratégia requer um portfólio conectado e segurança com metadados `Step`, `StepPrice` e `VolumeStep` válidos para cálculos de risco precisos.
- Os comandos são avaliados nas velas finalizadas, portanto, uma nova barra (ou atualização da vela) deve ser recebida após alternar um parâmetro manual.
- Ao usar distâncias avançadas, certifique-se de que `AdvancedStopPips` e `AdvancedTakeProfitPips` estejam preenchidos; caso contrário, os padrões baseados em ATR permanecerão em vigor.
