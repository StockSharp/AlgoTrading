# Estratégia Rotacional de Momentum por Classe de Ativo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este modelo rotacional aloca capital nas classes de ativos que exibem o maior momentum recente. A cada período o sistema classifica os ETF de ativos e mantém os líderes enquanto evita os retardatários.

O rebalanceamento ocorre mensalmente, usando caixa como ativo defensivo quando nenhum momentum é positivo.

## Detalhes

- **Dados**: Retornos totais mensais de ETF de classes de ativos.
- **Entrada**: Manter os N melhores ativos com momentum positivo.
- **Saída**: Substituir ativos quando saem do topo do ranking.
- **Instrumentos**: ETF amplos de classes de ativos.
- **Risco**: Usa proxy de caixa e limites de posição.

