# Estratégia Stochastic Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina uma entrada clássica com o oscilador Stochastic com uma média no estilo martingale.
Abre uma posição quando a linha %K cruza a linha %D e o oscilador está acima/abaixo de zonas configuráveis.
Se o preço se mover contra a posição um passo definido, a estratégia aumenta o volume por um multiplicador.
As posições são fechadas quando o lucro acumulado atinge um número definido de pontos.

## Detalhes
- **Critérios de entrada**
  - Comprado: %K > %D e %D > ZoneBuy
  - Vendido: %K < %D e %D < ZoneSell
- **Média**
  - Ordens adicionais são colocadas a cada `Step` pontos (ou `Step * número de ordens` no modo 1).
  - O volume de cada nova ordem é multiplicado por `Mult`.
- **Critérios de saída**
  - Comprado: preço ≥ último preço de compra + `ProfitFactor * número de ordens` pontos.
  - Vendido: preço ≤ último preço de venda − `ProfitFactor * número de ordens` pontos.
- **Parâmetros** incluem tamanho do passo, modo do passo, fator de lucro, multiplicador, volumes iniciais e períodos do Stochastic.
- **Filtros**
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Stochastic
  - Stops: Não
  - Complexidade: Médio
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
