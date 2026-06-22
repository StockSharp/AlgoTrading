# Estratégia Ma2Cci EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de cruzamento duplo de médias móveis exponenciais confirmada por uma ruptura da linha zero do Índice de Canal de Commodities (CCI). O tamanho da posição e a colocação do stop são derivados da volatilidade do Average True Range (ATR) e de uma porcentagem de risco configurável.

## Detalhes

- **Dados**: Velas baseadas em tempo (padrão 1 hora) fornecidas pelo parâmetro `Candle Type` selecionado.
- **Entrada**: Ir comprado quando a EMA rápida cruza acima da EMA lenta e o CCI cruza acima de zero na mesma barra; ir vendido no cruzamento oposto com o CCI rompendo abaixo de zero.
- **Saída**: Fechar comprados quando a EMA rápida cruza de volta abaixo da EMA lenta ou o preço toca o stop fixo; fechar vendidos quando a EMA rápida cruza acima da EMA lenta ou o preço atinge o stop vendido.
- **Risco**: A distância do stop equivale ao maior entre o ATR (comprimento `AtrPeriod`) ou `MinStopPoints` multiplicado pelo passo de preço do instrumento. O tamanho da operação é o valor da carteira vezes `RiskPercent`, dividido por essa distância de stop.
- **Instrumentos**: Símbolos de forex ou índices seguidores de tendência que suportam hedge na versão original do MetaTrader; também aplicável a outros ativos com oscilações de momentum claras.
- **Ambiente**: Projetado para mercados de sessão contínua onde os sinais EMA/CCI se alinham com os controles de risco baseados em ATR.

## Parâmetros

- `CandleType` – Período e tipo de dados utilizado para cálculos e fluxo de ordens.
- `FastMaPeriod` – Período da EMA rápida (padrão 10).
- `SlowMaPeriod` – Período da EMA lenta (padrão 37).
- `CciPeriod` – Lookback do oscilador CCI que confirma o momentum (padrão 39).
- `AtrPeriod` – Comprimento do ATR utilizado para estimar a volatilidade atual para a colocação de stops (padrão 3).
- `RiskPercent` – Fração do patrimônio da carteira atual arriscado por operação (padrão 2%).
- `MinStopPoints` – Distância mínima do stop expressa em passos de preço para emular o filtro de pips do MetaTrader (padrão 15).

## Notas

- Funciona melhor quando executado em pares líquidos e índices onde os cruzamentos EMA/CCI são confiáveis; mercados rasos podem acionar saídas prematuras.
- Como os stops são recalculados apenas na entrada, a estratégia mantém o perfil de risco estável e espelha a lógica de stop-loss fixo do especialista MQL original.
- A valoração da carteira deve ser fornecida pela conta conectada para que o dimensionamento de posição funcione; caso contrário, o motor recorre ao `Volume` da estratégia ou ao volume mínimo do instrumento.
