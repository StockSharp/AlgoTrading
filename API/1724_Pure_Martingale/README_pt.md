# Estratégia Martingale Puro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa um sistema de Martingale básico. Abre negociações em uma direção aleatória e dobra o tamanho da posição e a distância de stop/take após cada negociação perdedora. Após uma negociação vencedora, redefine para o volume e a distância iniciais.

A abordagem pressupõe que o preço eventualmente retornará à rentabilidade, mas o risco cresce exponencialmente. Use apenas em instrumentos líquidos com spreads reduzidos.

## Detalhes

- **Critérios de entrada**:
  - Sem posição aberta: comprar ou vender aleatoriamente no fechamento da vela.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Fechar quando o preço se mover a favor ou contra a posição pela distância configurada.
- **Stops**: Stop loss e take profit virtuais gerenciados pela estratégia.
- **Filtros**:
  - Nenhum.
