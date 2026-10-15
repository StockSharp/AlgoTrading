# Estratégia JBrainUltraRSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de exemplo combina o Índice de Força Relativa (RSI) e o oscilador Estocástico para gerar sinais de negociação.
A ideia é derivada do Consultor Especialista original do MetaTrader que usava os indicadores *JBrainTrendSig1* e *UltraRSI*. Nesta adaptação, o oscilador Estocástico atua como filtro de tendência enquanto o RSI fornece sinais de entrada.

## Como funciona

1. **Indicadores**
   - **RSI**: Mede o momentum comparando ganhos e perdas recentes. Um cruzamento acima do nível 50 indica momentum de alta, enquanto um cruzamento abaixo de 50 indica momentum de baixa.
   - **Oscilador Estocástico**: Avalia a posição do fechamento em relação ao intervalo recente. Os cruzamentos das linhas %K e %D confirmam a direção da tendência.
2. **Modos**
   - **JBrainSig1Filter** – O RSI gera sinais e o oscilador Estocástico confirma a direção.
   - **UltraRsiFilter** – O oscilador Estocástico fornece sinais filtrados pelo RSI.
   - **Composition** – Os sinais são tomados apenas quando ambos os indicadores concordam na direção.
3. **Regras de negociação**
   - Uma posição comprada é aberta quando um sinal de compra aparece e a posição vendida está ausente ou fechada.
   - Uma posição vendida é aberta quando um sinal de venda aparece e a posição comprada está ausente ou fechada.
   - Sinais reversos fecham posições existentes se permitido.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `RsiPeriod` | Período de cálculo do RSI. |
| `StochLength` | Período %K para o oscilador Estocástico. |
| `SignalLength` | Período %D para o oscilador Estocástico. |
| `Mode` | Modo de combinação de indicadores. |
| `AllowLongEntry` / `AllowShortEntry` | Permissões para abrir posições compradas ou vendidas. |
| `AllowLongExit` / `AllowShortExit` | Permissões para fechar posições compradas ou vendidas. |
| `CandleType` | Período de velas utilizado pela estratégia. |

## Notas

- A estratégia usa a API de alto nível do StockSharp com `Bind` / `BindEx` para o processamento de indicadores.
- Stops e alvos podem ser configurados com o mecanismo de proteção integrado `StartProtection()`.
- A visualização de exemplo desenha velas, indicadores e operações próprias se uma área de gráfico estiver disponível.
