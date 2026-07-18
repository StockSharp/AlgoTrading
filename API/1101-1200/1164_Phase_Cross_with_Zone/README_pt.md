# Estratégia de Cruzamento de Fase com Zona
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de exemplo entra comprado quando uma SMA suavizada com deslocamento positivo cruza acima de uma EMA com deslocamento negativo. A posição é fechada quando ocorre o cruzamento oposto.

## Detalhes

- **Critérios de entrada**: SMA + deslocamento cruza acima de EMA - deslocamento.
- **Comprado/Vendido**: somente comprado.
- **Critérios de saída**: cruzamento oposto.
- **Stops**: nenhum.
- **Valores padrão**:
  - `Length` = 20.
  - `Offset` = 0.5.
- **Filtros**: nenhum.
- **Complexidade**: baixa.
- **Período**: configurável.
