# Estratégia Fibonacci Auto Trend Scouter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza dois extremos móveis baseados em números Fibonacci para rastrear tendências emergentes. A janela curta (8) acompanha máximas e mínimas recentes, enquanto a janela longa (21) fornece contexto. Uma posição comprada é aberta quando a máxima de curto prazo supera a máxima de longo prazo. Uma posição vendida é aberta quando a mínima de curto prazo cai abaixo da mínima de longo prazo.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: máxima de curto prazo > máxima de longo prazo.
  - **Vendido**: mínima de curto prazo < mínima de longo prazo.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - A posição é revertida no sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `Short period` = 8
  - `Long period` = 21
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Não
  - Complexidade: Simples
  - Período: Médio prazo
