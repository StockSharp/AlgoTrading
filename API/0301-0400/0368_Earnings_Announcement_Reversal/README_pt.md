# Reversão por Anúncio de Resultados
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de **Reversão por Anúncio de Resultados** vende a descoberto os vencedores recentes e compra os perdedores recentes nos dias de anúncio de resultados.

## Detalhes
- **Critérios de entrada**: No dia dos resultados, vender a descoberto ações com retornos recentes positivos e comprar as de retornos negativos.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Posição ajustada após o sinal; sem regra de manutenção explícita.
- **Stops**: Não.
- **Valores padrão**:
  - `LookbackDays = 5`
  - `HoldingDays = 3`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Event-driven
  - Direção: Ambos
  - Indicadores: Returns
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
