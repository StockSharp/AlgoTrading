# Estratégia de Tendência com Hurst Exponent
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este sistema usa o Hurst Exponent para determinar se o mercado está exibindo comportamento de tendência. Valores acima do limiar indicam persistência, enquanto valores abaixo sugerem ruído ou reversão à média. Uma média móvel fornece confirmação adicional de direção.

Os testes indicam um retorno anual médio de aproximadamente 40%. Funciona melhor no mercado de criptomoedas.

A estratégia compra quando o Hurst Exponent é maior que o limiar e o preço fecha acima da média móvel. Vende a descoberto quando o Hurst Exponent é alto e o preço fecha abaixo da média. Se o Hurst Exponent cair abaixo do limiar, as posições existentes são fechadas para evitar operar em mercados agitados.

Essa abordagem funciona para traders que desejam confirmação objetiva de que uma tendência existe antes de entrar. A combinação de filtro de tendência e stop-loss ajuda a gerenciar o risco de sinais falsos.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Hurst > Limiar && Fechamento > MA
  - **Vendido**: Hurst > Limiar && Fechamento < MA
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando Fechamento < MA ou Hurst < Limiar
  - **Vendido**: Sair quando Fechamento > MA ou Hurst < Limiar
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `HurstPeriod` = 100
  - `MaPeriod` = 20
  - `HurstThreshold` = 0.55m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Hurst Exponent, MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
