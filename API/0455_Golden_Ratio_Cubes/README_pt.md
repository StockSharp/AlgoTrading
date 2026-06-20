# Estratégia Golden Ratio Cubes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Golden Ratio Cubes usa matemática de Fibonacci para detectar rompimentos.
Ela rastreia o máximo mais alto e o mínimo mais baixo durante uma janela de referência
e calcula extensões baseadas na razão áurea (φ ≈ 1.618). Quando o preço fecha além
dessas extensões, a estratégia entra na direção do rompimento.

## Detalhes

- **Critérios de entrada**:
  - Fechamento acima da extensão da razão áurea do range recente → Comprar.
  - Fechamento abaixo da extensão da razão áurea do range recente → Vender.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Sinal de rompimento oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Lookback` = 34
  - `Phi` = 1.618
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Comprado e Vendido
  - Indicadores: Highest, Lowest
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
