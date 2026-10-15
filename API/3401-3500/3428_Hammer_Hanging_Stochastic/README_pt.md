# Suspensão de martelo Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia transporta o MetaTrader especialista "Expert_AH_HM_Stoch" para o StockSharp API de alto nível. Ele combina padrões de velas de martelo e homem pendurado com confirmação de oscilador estocástico para capturar configurações de reversão após movimentos prolongados.

A estratégia espera por uma vela completa antes de agir, usa a linha de sinal estocástica para filtragem e fecha posições quando o impulso sai das zonas extremas.

## Detalhes

- **Critérios de entrada**:
  - Longo: Vela martelo de alta e %D estocástico (barra anterior) abaixo do nível de sobrevenda.
  - Venda: Vela de baixa pendente e %D estocástico (barra anterior) acima do nível de sobrecompra.
- **Longo/Curto**: Ambos.
- **Critérios de saída**: Fechar posições quando %D estocástico cruzar acima/abaixo da recuperação configurável e níveis extremos.
- **Paradas**: ativado por meio do gancho `StartProtection()` integrado (o padrão é proteção em nível de conta).
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromHours(1)
  - `StochPeriodK` = 15
  - `StochPeriodD` = 49
  - `StochPeriodSlow` = 25
  - `OversoldLevel` = 30
  - `OverboughtLevel` = 70
  - `ExitLowerLevel` = 20
  - `ExitUpperLevel` = 80
  - `MaxBodyRatio` = 0,35
  - `LowerShadowMultiplier` = 2,5
  - `UpperShadowMultiplier` = 0,3
- **Filtros**:
  - Categoria: Confirmação de Padrão + Oscilador
  - Direção: Ambos
  - Indicadores: Castiçal, Stochastic
  - Paradas: controles de risco opcionais via `StartProtection`
  - Complexidade: Intermediário
  - Prazo: Swing / Intraday (padrão 1h)
  - Sazonalidade: Não
  - Redes Neurais: Não
  - Divergência: Não
  - Nível de risco: moderado

## Como funciona

1. Assina a série de velas configurada e o oscilador estocástico usando o `BindEx` API de alto nível.
2. Detecta formações de martelos e homens pendurados com base nas proporções de corpo e sombra.
3. Confirma as entradas com a linha %D estocástica usando o valor da barra fechada anterior.
4. Gerencia saídas quando o estocástico sai das zonas de sobrevenda/sobrecompra, espelhando a lógica do especialista MQL original.
5. Fornece visualização de gráficos para velas, estocásticos e negociações próprias quando uma área do gráfico está disponível.
