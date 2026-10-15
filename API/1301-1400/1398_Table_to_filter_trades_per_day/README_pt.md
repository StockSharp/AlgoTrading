# Estratégia de Tabela para Filtrar Operações por Dia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia simples de cruzamento de médias móveis usando SMA50 e SMA200 com alvos fixos de lucro e perda.

## Detalhes

- **Entrada**
  - Comprado: SMA50 cruza acima de SMA200.
  - Vendido: SMA50 cruza abaixo de SMA200.
- **Saída**: fechar posição quando o alvo ou o stop for atingido.
