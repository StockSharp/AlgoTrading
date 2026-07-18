# Estratégia Martini Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa uma grade de martingale com hedge. Começa colocando ordens stop em ambos os lados do preço atual e dobra o tamanho da posição na direção oposta sempre que o mercado se move contra a exposição atual em um passo especificado. Todas as operações são fechadas quando o lucro acumulado excede o alvo.

## Detalhes

- **Critérios de entrada**:
  - Colocar um buy stop acima e um sell stop abaixo do mercado à distância `Step`.
  - Quando uma ordem é acionada, cancelar o stop oposto.
- **Gestão de posição**:
  - Rastrear o preço da última ordem executada.
  - Se o preço se mover contra a posição aberta em `Step * orderCount`, enviar uma ordem de mercado na direção oposta com o dobro do volume anterior.
- **Critérios de saída**:
  - Fechar todas as posições quando o lucro não realizado atingir `ProfitClose`.
- **Comprado/Vendido**: Ambos.
- **Stops**: Usa ordens stop para entradas iniciais; sem stop-loss.
- **Indicadores**: Nenhum.
- **Filtros**: Nenhum.

### Parâmetros

- `Step` – passo de preço em unidades absolutas.
- `ProfitClose` – limite de lucro para fechar todas as operações.
- `InitialVolume` – volume inicial para a primeira ordem.
- `CandleType` – série de velas usada para atualizações de preço.
