# Estratégia Flawless Victory
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Flawless Victory é um sistema de momentum modular que combina osciladores com Bandas de Bollinger. Dependendo da versão selecionada, pode operar com sinais simples de RSI, aplicar alvos fixos de take-profit e stop-loss, ou exigir confirmação do Money Flow Index. O objetivo é explorar o esgotamento nas bordas das bandas de volatilidade e aproveitar as oscilações de reversão à média.

A versão 1 entra quando o RSI sai de zonas de sobrevendido ou sobrecomprado perto dos extremos de Bollinger. A versão 2 adiciona controle explícito de risco via alvos baseados em percentuais. A versão 3 exige que tanto o RSI quanto o MFI concordem, filtrando reversões fracas.

A estratégia tem melhor desempenho em mercados intradiários com limites de volatilidade claros.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: ver regras da versão (RSI <30 perto da banda inferior; versão 3 também `MFI < 20`)
  - **Vendido**: RSI >70 perto da banda superior (versão 3 também `MFI > 80`)
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - **Versão 1**: sinal RSI oposto
  - **Versão 2**: percentuais de take-profit ou stop-loss
  - **Versão 3**: combinação oposta RSI/MFI
- **Stops**: Opcional na versão 2
- **Valores padrão**:
  - `RSI_length` = 14
  - `MFI_length` = 14
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `TakeProfitPct` = 1.5
  - `StopLossPct` = 1.0
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: RSI, MFI, Bollinger Bands
  - Stops: Opcional
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
