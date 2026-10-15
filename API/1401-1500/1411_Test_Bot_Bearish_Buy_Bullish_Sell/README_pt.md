# Bot de Teste: Compra em Baixista / Venda em Altista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Entra comprado na primeira vela de baixa e sai na primeira vela de alta.

## Detalhes

- **Critérios de entrada**: Primeira vela de baixa quando sem posição.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Primeira vela de alta.
- **Stops**: Não.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Reversão
  - Direção: Comprado
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
